using Butcher.Api.Application.Dtos;
using Butcher.Api.Application.Services;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Butcher.Api.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Tests.Application.Services;

[Collection(DatabaseCollection.Name)]
public class StockMovementServiceTests(PostgresDatabaseFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<StockUnit> SeedStockUnitAsync(
        AppDbContext dbContext, SaleMode saleMode, decimal? weight, string code = "SC")
    {
        var product = new Product { Code = code, Name = "Saucisse curry", SaleMode = saleMode, AllowPartialSale = true };
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        var batch = new ProductionBatch
        {
            ProductId = product.Id,
            ProductionDate = new DateOnly(2026, 8, 31),
            SalePrice = 12.5m,
        };
        dbContext.ProductionBatches.Add(batch);
        await dbContext.SaveChangesAsync();

        var stockUnit = new StockUnit { UnitNumber = TestUnitNumber.Next(), BatchId = batch.Id, Weight = weight };
        dbContext.StockUnits.Add(stockUnit);
        await dbContext.SaveChangesAsync();
        return stockUnit;
    }

    private static async Task<Customer> SeedCustomerAsync(AppDbContext dbContext)
    {
        var customer = new Customer { LastName = "Dupont", FirstName = "Jean" };
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();
        return customer;
    }

    /// <summary>
    /// Une vente est désormais toujours rattachée à une <c>Sale</c>, qui porte le client obligatoire
    /// (RF-17 / RG-07). Ce helper crée l'enveloppe attendue par les mouvements de type « vente ».
    /// </summary>
    private static async Task<Sale> SeedSaleAsync(AppDbContext dbContext, string saleNumber = "V-260904-1")
    {
        var customer = await SeedCustomerAsync(dbContext);

        var sale = new Sale
        {
            SaleNumber = saleNumber,
            CustomerId = customer.Id,
            Date = DateTimeOffset.UtcNow,
        };
        dbContext.Sales.Add(sale);
        await dbContext.SaveChangesAsync();
        return sale;
    }

    [Fact]
    public async Task CreateAsync_FullSaleByWeight_SetsUnitSoldAndStoresAmount()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByWeight, weight: 0.320m);
        var sale = await SeedSaleAsync(dbContext);
        var service = new StockMovementService(dbContext);

        var result = await service.CreateAsync(unit.Id, new CreateStockMovementRequest
        {
            Type = MovementType.Sale,
            IsFullSale = true,
            SoldWeight = 0.320m,
            Amount = 4.5m,
            SaleId = sale.Id,
        });

        Assert.Equal(MovementType.Sale, result.Type);
        Assert.Equal(4.5m, result.Amount);
        Assert.Equal(sale.Id, result.SaleId);
        Assert.Equal("V-260904-1", result.SaleNumber);
        Assert.Equal("Jean Dupont", result.CustomerName);
        Assert.Equal("Saucisse curry", result.ProductName);
        // Le mouvement nomme l'unité par l'étiquette que l'utilisateur a sous les yeux (FR-009).
        Assert.Equal(unit.UnitNumber, result.UnitNumber);

        var updatedUnit = await dbContext.StockUnits.FindAsync(unit.Id);
        Assert.Equal(StockUnitStatus.Sold, updatedUnit!.Status);
    }

    [Fact]
    public async Task CreateAsync_PartialSale_SetsUnitOpened_AndSubsequentSaleStaysOpened()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByWeight, weight: null);
        var sale = await SeedSaleAsync(dbContext);
        var service = new StockMovementService(dbContext);

        await service.CreateAsync(unit.Id, new CreateStockMovementRequest
        {
            Type = MovementType.Sale,
            IsFullSale = false,
            SoldWeight = 0.150m,
            Amount = 2m,
            SaleId = sale.Id,
        });

        var afterFirst = await dbContext.StockUnits.FindAsync(unit.Id);
        Assert.Equal(StockUnitStatus.Opened, afterFirst!.Status);

        // isFullSale=true est ignoré une fois l'unité déjà "opened" : elle reste ouverte tant qu'elle
        // n'est pas clôturée manuellement (RG-04).
        await service.CreateAsync(unit.Id, new CreateStockMovementRequest
        {
            Type = MovementType.Sale,
            IsFullSale = true,
            SoldWeight = 0.100m,
            Amount = 1.3m,
            SaleId = sale.Id,
        });

        var afterSecond = await dbContext.StockUnits.FindAsync(unit.Id);
        Assert.Equal(StockUnitStatus.Opened, afterSecond!.Status);
    }

    // RG-05 : le poids restant n'est pas suivi, mais la somme des tranches vendues ne peut pas
    // dépasser le poids physique pesé de l'unité (1 kg ici).
    [Fact]
    public async Task CreateAsync_PartialSale_ExceedingUnitWeight_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByWeight, weight: 1.000m);
        var sale = await SeedSaleAsync(dbContext);
        var service = new StockMovementService(dbContext);

        await service.CreateAsync(unit.Id, new CreateStockMovementRequest
        {
            Type = MovementType.Sale,
            IsFullSale = false,
            SoldWeight = 0.230m,
            Amount = 3m,
            SaleId = sale.Id,
        });

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(unit.Id, new CreateStockMovementRequest
        {
            Type = MovementType.Sale,
            IsFullSale = false,
            SoldWeight = 1.000m,
            Amount = 12m,
            SaleId = sale.Id,
        }));

        var afterRejection = await dbContext.StockUnits.FindAsync(unit.Id);
        Assert.Equal(StockUnitStatus.Opened, afterRejection!.Status);
    }

    [Fact]
    public async Task UpdateAsync_SoldWeight_ExceedingUnitWeight_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByWeight, weight: 1.000m);
        var sale = await SeedSaleAsync(dbContext);
        var service = new StockMovementService(dbContext);

        await service.CreateAsync(unit.Id, new CreateStockMovementRequest
        {
            Type = MovementType.Sale,
            IsFullSale = false,
            SoldWeight = 0.230m,
            Amount = 3m,
            SaleId = sale.Id,
        });
        var second = await service.CreateAsync(unit.Id, new CreateStockMovementRequest
        {
            Type = MovementType.Sale,
            IsFullSale = false,
            SoldWeight = 0.300m,
            Amount = 4m,
            SaleId = sale.Id,
        });

        // 0.230 (première ligne, non modifiée) + 0.800 (nouvelle valeur) > 1.000 kg.
        await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateAsync(second.Id, new UpdateStockMovementRequest { SoldWeight = 0.800m, Amount = 4m }));
    }

    // Le produit ne remplit AllowPartialSale que dans SeedStockUnitAsync ; ici on le désactive
    // explicitement pour vérifier le garde-fou serveur.
    [Fact]
    public async Task CreateAsync_PartialSale_OnProductWithoutAllowPartialSale_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByWeight, weight: null);
        var product = await dbContext.Products.SingleAsync(p => p.Code == "SC");
        product.AllowPartialSale = false;
        await dbContext.SaveChangesAsync();
        var sale = await SeedSaleAsync(dbContext);
        var service = new StockMovementService(dbContext);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(unit.Id, new CreateStockMovementRequest
        {
            Type = MovementType.Sale,
            IsFullSale = false,
            SoldWeight = 0.150m,
            Amount = 2m,
            SaleId = sale.Id,
        }));
    }

    [Fact]
    public async Task CloseAsync_OnOpenedUnit_SetsSold()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByWeight, weight: null);
        var sale = await SeedSaleAsync(dbContext);
        var service = new StockMovementService(dbContext);
        await service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Sale, IsFullSale = false, SoldWeight = 0.150m, Amount = 2m, SaleId = sale.Id });

        await service.CloseAsync(unit.Id);

        var closedUnit = await dbContext.StockUnits.FindAsync(unit.Id);
        Assert.Equal(StockUnitStatus.Sold, closedUnit!.Status);
    }

    [Fact]
    public async Task CloseAsync_OnAvailableUnit_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByWeight, weight: 0.320m);
        var service = new StockMovementService(dbContext);

        await Assert.ThrowsAsync<ConflictException>(() => service.CloseAsync(unit.Id));
    }

    [Fact]
    public async Task CreateAsync_PersonalFromOpened_SetsPersonal()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByWeight, weight: null);
        var sale = await SeedSaleAsync(dbContext);
        var service = new StockMovementService(dbContext);
        await service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Sale, IsFullSale = false, SoldWeight = 0.150m, Amount = 2m, SaleId = sale.Id });

        await service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Personal, SoldWeight = 0.170m });

        var updatedUnit = await dbContext.StockUnits.FindAsync(unit.Id);
        Assert.Equal(StockUnitStatus.Personal, updatedUnit!.Status);
    }

    [Fact]
    public async Task CreateAsync_OnAlreadyFinalizedUnit_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByWeight, weight: 0.320m);
        var sale = await SeedSaleAsync(dbContext);
        var service = new StockMovementService(dbContext);
        await service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Sale, IsFullSale = true, SoldWeight = 0.320m, Amount = 4m, SaleId = sale.Id });

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Personal, SoldWeight = 0.1m }));
    }

    [Fact]
    public async Task CreateAsync_SaleWithoutAmount_ThrowsBadRequestException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByWeight, weight: 0.320m);
        var sale = await SeedSaleAsync(dbContext);
        var service = new StockMovementService(dbContext);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Sale, SoldWeight = 0.320m, SaleId = sale.Id }));
    }

    [Fact]
    public async Task CreateAsync_PersonalWithAmount_ThrowsBadRequestException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByWeight, weight: 0.320m);
        var service = new StockMovementService(dbContext);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Personal, SoldWeight = 0.320m, Amount = 4m }));
    }

    [Fact]
    public async Task CreateAsync_ByPieceWithSoldWeight_ThrowsBadRequestException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByPiece, weight: null);
        var sale = await SeedSaleAsync(dbContext);
        var service = new StockMovementService(dbContext);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Sale, IsFullSale = true, Amount = 5m, SoldWeight = 0.1m, SaleId = sale.Id }));
    }

    // RF-17 / RG-07 (modifiés le 2026-09-04) : une vente sans client n'existe plus. Structurellement,
    // pas de vente sans Sale — et une Sale porte toujours un client.
    [Fact]
    public async Task CreateAsync_SaleWithoutSaleId_ThrowsBadRequestException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByPiece, weight: null);
        var service = new StockMovementService(dbContext);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Sale, IsFullSale = true, Amount = 5m }));
    }

    [Fact]
    public async Task CreateAsync_PersonalWithSaleId_ThrowsBadRequestException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByPiece, weight: null);
        var sale = await SeedSaleAsync(dbContext);
        var service = new StockMovementService(dbContext);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Personal, SaleId = sale.Id }));
    }

    [Fact]
    public async Task CreateAsync_WithUnknownSale_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByPiece, weight: null);
        var service = new StockMovementService(dbContext);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Sale, IsFullSale = true, Amount = 5m, SaleId = 999 }));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesAmountAndNotes()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByPiece, weight: null);
        var sale = await SeedSaleAsync(dbContext);
        var service = new StockMovementService(dbContext);
        var created = await service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Sale, IsFullSale = true, Amount = 5m, SaleId = sale.Id });

        var updated = await service.UpdateAsync(created.Id, new UpdateStockMovementRequest { Amount = 5.5m, Notes = "Remise fidélité" });

        Assert.Equal(5.5m, updated.Amount);
        Assert.Equal("Remise fidélité", updated.Notes);
    }

    [Fact]
    public async Task DeleteAsync_OnlyMovement_RevertsUnitToAvailable()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByPiece, weight: null);
        var service = new StockMovementService(dbContext);
        var created = await service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Personal });

        await service.DeleteAsync(created.Id);

        var updatedUnit = await dbContext.StockUnits.FindAsync(unit.Id);
        Assert.Equal(StockUnitStatus.Available, updatedUnit!.Status);
    }

    // Supprimer la dernière ligne d'une vente laisserait une vente vide : on renvoie l'utilisateur
    // vers DELETE /api/sales/{id}, qui est explicite.
    [Fact]
    public async Task DeleteAsync_LastLineOfSale_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByPiece, weight: null);
        var sale = await SeedSaleAsync(dbContext);
        var service = new StockMovementService(dbContext);
        var created = await service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Sale, IsFullSale = true, Amount = 5m, SaleId = sale.Id });

        await Assert.ThrowsAsync<ConflictException>(() => service.DeleteAsync(created.Id));
    }

    [Fact]
    public async Task DeleteAsync_OneOfSeveralPartialMovements_KeepsUnitOpened()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByWeight, weight: null);
        var sale = await SeedSaleAsync(dbContext);
        var service = new StockMovementService(dbContext);
        var first = await service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Sale, IsFullSale = false, SoldWeight = 0.150m, Amount = 2m, SaleId = sale.Id });
        await service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Sale, IsFullSale = false, SoldWeight = 0.100m, Amount = 1.3m, SaleId = sale.Id });

        await service.DeleteAsync(first.Id);

        var updatedUnit = await dbContext.StockUnits.FindAsync(unit.Id);
        Assert.Equal(StockUnitStatus.Opened, updatedUnit!.Status);
    }

    [Fact]
    public async Task GetAllAsync_FilteredByStockUnitId_ReturnsOnlyMatchingMovements()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit1 = await SeedStockUnitAsync(dbContext, SaleMode.ByPiece, weight: null, code: "SC");
        var unit2 = await SeedStockUnitAsync(dbContext, SaleMode.ByPiece, weight: null, code: "JB");
        var sale = await SeedSaleAsync(dbContext);
        var service = new StockMovementService(dbContext);
        await service.CreateAsync(unit1.Id, new CreateStockMovementRequest { Type = MovementType.Sale, IsFullSale = true, Amount = 5m, SaleId = sale.Id });
        await service.CreateAsync(unit2.Id, new CreateStockMovementRequest { Type = MovementType.Sale, IsFullSale = true, Amount = 6m, SaleId = sale.Id });

        var result = await service.GetAllAsync(unit1.Id, customerId: null, saleId: null);

        Assert.Single(result);
        Assert.Equal(unit1.Id, result[0].StockUnitId);
    }

    [Fact]
    public async Task GetAllAsync_FilteredByCustomerId_ResolvesThroughTheSale()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit1 = await SeedStockUnitAsync(dbContext, SaleMode.ByPiece, weight: null, code: "SC");
        var unit2 = await SeedStockUnitAsync(dbContext, SaleMode.ByPiece, weight: null, code: "JB");
        var sale = await SeedSaleAsync(dbContext);
        var service = new StockMovementService(dbContext);
        await service.CreateAsync(unit1.Id, new CreateStockMovementRequest { Type = MovementType.Sale, IsFullSale = true, Amount = 5m, SaleId = sale.Id });
        await service.CreateAsync(unit2.Id, new CreateStockMovementRequest { Type = MovementType.Personal });

        var result = await service.GetAllAsync(stockUnitId: null, customerId: sale.CustomerId, saleId: null);

        Assert.Single(result);
        Assert.Equal(unit1.Id, result[0].StockUnitId);
        Assert.Equal(sale.CustomerId, result[0].CustomerId);
        Assert.Equal(unit1.UnitNumber, result[0].UnitNumber);
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ThrowsNotFoundException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new StockMovementService(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(999));
    }

    // --- Poids d'une sortie perso ou perte : calculé par le serveur (FR-020) ---------------------

    [Fact]
    public async Task CreateAsync_LossOnAvailableUnit_RecordsFullWeighedWeight()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByWeight, weight: 1.250m);
        var service = new StockMovementService(dbContext);

        var result = await service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Loss });

        Assert.Equal(1.250m, result.SoldWeight);
    }

    // Sur une unité entamée, repartir du poids pesé compterait deux fois la part déjà vendue.
    [Fact]
    public async Task CreateAsync_PersonalOnOpenedUnit_RecordsRemainingWeightOnly()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByWeight, weight: 5m);
        var sale = await SeedSaleAsync(dbContext);
        var service = new StockMovementService(dbContext);
        await service.CreateAsync(unit.Id, new CreateStockMovementRequest
        {
            Type = MovementType.Sale,
            SoldWeight = 2m,
            Amount = 30m,
            SaleId = sale.Id,
            IsFullSale = false,
        });

        var result = await service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Personal });

        Assert.Equal(3m, result.SoldWeight);
    }

    [Fact]
    public async Task CreateAsync_LossOnUnitWithoutWeight_RecordsNoWeight()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByPiece, weight: null);
        var service = new StockMovementService(dbContext);

        var result = await service.CreateAsync(unit.Id, new CreateStockMovementRequest { Type = MovementType.Loss });

        Assert.Null(result.SoldWeight);
    }

    // Le client n'est plus l'auteur de ce poids : ce qu'il envoie sur une sortie perso ou perte est
    // ignoré au profit du calcul serveur.
    [Fact]
    public async Task CreateAsync_LossWithClientSuppliedWeight_IgnoresItAndUsesServerRule()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByWeight, weight: 1.250m);
        var service = new StockMovementService(dbContext);

        var result = await service.CreateAsync(unit.Id, new CreateStockMovementRequest
        {
            Type = MovementType.Loss,
            SoldWeight = 99m,
        });

        Assert.Equal(1.250m, result.SoldWeight);
    }

    // Une unité au poids pas encore pesée : le serveur n'a rien à calculer, le poids saisi par
    // l'utilisateur reste la seule information disponible.
    [Fact]
    public async Task CreateAsync_LossOnUnweighedByWeightUnit_KeepsUserSuppliedWeight()
    {
        await using var dbContext = fixture.CreateDbContext();
        var unit = await SeedStockUnitAsync(dbContext, SaleMode.ByWeight, weight: null);
        var service = new StockMovementService(dbContext);

        var result = await service.CreateAsync(unit.Id, new CreateStockMovementRequest
        {
            Type = MovementType.Loss,
            SoldWeight = 0.900m,
        });

        Assert.Equal(0.900m, result.SoldWeight);
    }
}
