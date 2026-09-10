using Butcher.Api.Application.Dtos;
using Butcher.Api.Application.Services;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Tests.Application.Services;

[Collection(DatabaseCollection.Name)]
public class ProductServiceTests(PostgresDatabaseFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // Un produit se réduit à code / name / sale_mode / is_active : l'unité de vente a été retirée
    // du périmètre V1 (décision 2026-09-04), le mode de vente suffit à piloter l'affichage du prix.
    [Fact]
    public async Task CreateAsync_CreatesProduct()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);

        var result = await service.CreateAsync(new CreateProductRequest
        {
            Code = "SC",
            Name = "Saucisse curry",
            SaleMode = SaleMode.ByWeight,
        });

        Assert.True(result.Id > 0);
        Assert.Equal("SC", result.Code);
        Assert.Equal("Saucisse curry", result.Name);
        Assert.Equal(SaleMode.ByWeight, result.SaleMode);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_ByWeightWithAllowPartialSale_Succeeds()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);

        var result = await service.CreateAsync(new CreateProductRequest
        {
            Code = "JB",
            Name = "Jambon",
            SaleMode = SaleMode.ByWeight,
            AllowPartialSale = true,
        });

        Assert.True(result.AllowPartialSale);
    }

    // La vente à la tranche n'a de sens que pour un produit vendu au poids (RF-19).
    [Fact]
    public async Task CreateAsync_ByPieceWithAllowPartialSale_ThrowsBadRequestException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(new CreateProductRequest
        {
            Code = "TR",
            Name = "Terrine",
            SaleMode = SaleMode.ByPiece,
            AllowPartialSale = true,
        }));
    }

    [Fact]
    public async Task UpdateAsync_CanToggleAllowPartialSale_ForByWeightProduct()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "JB", Name = "Jambon", SaleMode = SaleMode.ByWeight });

        var updated = await service.UpdateAsync(created.Id, Update(created, allowPartialSale: true));

        Assert.True(updated.AllowPartialSale);
    }

    [Fact]
    public async Task UpdateAsync_ByPieceWithAllowPartialSale_ThrowsBadRequestException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "TR", Name = "Terrine", SaleMode = SaleMode.ByPiece });

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.UpdateAsync(created.Id, Update(created, allowPartialSale: true)));
    }

    // Création possible sur une base entièrement vide : plus aucun référentiel à alimenter d'abord.
    [Fact]
    public async Task CreateAsync_OnEmptyDatabase_Succeeds()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);

        var result = await service.CreateAsync(new CreateProductRequest
        {
            Code = "TR",
            Name = "Terrine",
            SaleMode = SaleMode.ByPiece,
        });

        Assert.True(result.Id > 0);
        Assert.Equal(SaleMode.ByPiece, result.SaleMode);
    }

    [Fact]
    public async Task CreateAsync_WithLowercaseCode_NormalizesToUppercase()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);

        var result = await service.CreateAsync(new CreateProductRequest
        {
            Code = "sc",
            Name = "Saucisse curry",
            SaleMode = SaleMode.ByWeight,
        });

        Assert.Equal("SC", result.Code);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateCode_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisse curry", SaleMode = SaleMode.ByWeight });

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Autre produit", SaleMode = SaleMode.ByWeight }));
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ThrowsNotFoundException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(999));
    }

    [Fact]
    public async Task GetAllAsync_ByDefault_ReturnsOnlyActiveProducts()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var active = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisse curry", SaleMode = SaleMode.ByWeight });
        var inactive = await service.CreateAsync(new CreateProductRequest { Code = "JB", Name = "Jambon", SaleMode = SaleMode.ByWeight });
        await service.DeactivateAsync(inactive.Id);

        var result = await service.GetAllAsync(includeInactive: false);

        Assert.Single(result);
        Assert.Equal(active.Id, result[0].Id);
    }

    [Fact]
    public async Task GetAllAsync_WithIncludeInactive_ReturnsAllProducts()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisse curry", SaleMode = SaleMode.ByWeight });
        var inactive = await service.CreateAsync(new CreateProductRequest { Code = "JB", Name = "Jambon", SaleMode = SaleMode.ByWeight });
        await service.DeactivateAsync(inactive.Id);

        var result = await service.GetAllAsync(includeInactive: true);

        Assert.Equal(2, result.Count);
    }

    // Un produit qui n'a jamais servi se corrige entièrement : c'est la demande des utilisateurs
    // tests, une erreur de saisie repérée dans les minutes qui suivent la création (FR-003).
    [Fact]
    public async Task UpdateAsync_OnNeverUsedProduct_UpdatesEveryDescriptiveField()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisson", SaleMode = SaleMode.ByPiece });

        var updated = await service.UpdateAsync(created.Id, new UpdateProductRequest
        {
            Name = "Saucisson sec",
            Code = "SEC",
            SaleMode = SaleMode.ByWeight,
            AllowPartialSale = true,
        });

        Assert.Equal("Saucisson sec", updated.Name);
        Assert.Equal("SEC", updated.Code);
        Assert.Equal(SaleMode.ByWeight, updated.SaleMode);
        Assert.True(updated.AllowPartialSale);
        Assert.False(updated.IsUsed);
    }

    [Fact]
    public async Task UpdateAsync_WithLowercaseCode_NormalizesToUppercase()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisson", SaleMode = SaleMode.ByWeight });

        var updated = await service.UpdateAsync(created.Id, Update(created, code: "sec"));

        Assert.Equal("SEC", updated.Code);
    }

    // L'unicité du code vaut désormais aussi à la modification, et sur les produits désactivés
    // comme actifs (FR-007).
    [Fact]
    public async Task UpdateAsync_WithCodeOfAnotherProduct_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        await service.CreateAsync(new CreateProductRequest { Code = "JB", Name = "Jambon", SaleMode = SaleMode.ByWeight });
        var target = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisson", SaleMode = SaleMode.ByWeight });

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateAsync(target.Id, Update(target, code: "jb")));
    }

    [Fact]
    public async Task UpdateAsync_WithCodeOfDeactivatedProduct_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var retired = await service.CreateAsync(new CreateProductRequest { Code = "JB", Name = "Jambon", SaleMode = SaleMode.ByWeight });
        await service.DeactivateAsync(retired.Id);
        var target = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisson", SaleMode = SaleMode.ByWeight });

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateAsync(target.Id, Update(target, code: "JB")));
    }

    [Fact]
    public async Task UpdateAsync_WithItsOwnCode_Succeeds()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisson", SaleMode = SaleMode.ByWeight });

        var updated = await service.UpdateAsync(created.Id, Update(created, name: "Saucisson sec"));

        Assert.Equal("Saucisson sec", updated.Name);
    }

    // Le passage au poids vers la pièce éteint la vente à la tranche, les deux étant incompatibles.
    // Sans cela la validation, qui lisait le mode de vente en base, laisserait passer la
    // combinaison interdite dans une requête unique (FR-008, FR-009).
    [Fact]
    public async Task UpdateAsync_SwitchingToByPiece_TurnsOffAllowPartialSale()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest
        {
            Code = "JB",
            Name = "Jambon",
            SaleMode = SaleMode.ByWeight,
            AllowPartialSale = true,
        });

        var updated = await service.UpdateAsync(created.Id, new UpdateProductRequest
        {
            Name = "Jambon",
            Code = "JB",
            SaleMode = SaleMode.ByPiece,
            AllowPartialSale = true,
        });

        Assert.Equal(SaleMode.ByPiece, updated.SaleMode);
        Assert.False(updated.AllowPartialSale);
    }

    // --- Produit déjà utilisé : le code et le mode de vente sont figés (FR-004, FR-005) ---------

    [Fact]
    public async Task UpdateAsync_OnUsedProduct_ChangingCode_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisson", SaleMode = SaleMode.ByWeight });
        await AddBatchAsync(dbContext, created.Id);

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateAsync(created.Id, Update(created, code: "SEC")));

        var unchanged = await service.GetByIdAsync(created.Id);
        Assert.Equal("SC", unchanged.Code);
    }

    [Fact]
    public async Task UpdateAsync_OnUsedProduct_ChangingSaleMode_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisson", SaleMode = SaleMode.ByWeight });
        await AddBatchAsync(dbContext, created.Id);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateAsync(created.Id, Update(created, saleMode: SaleMode.ByPiece)));
    }

    [Fact]
    public async Task UpdateAsync_OnUsedProduct_ChangingNameOnly_Succeeds()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisson", SaleMode = SaleMode.ByWeight });
        await AddBatchAsync(dbContext, created.Id);

        var updated = await service.UpdateAsync(created.Id, Update(created, name: "Saucisson sec"));

        Assert.Equal("Saucisson sec", updated.Name);
        Assert.Equal("SC", updated.Code);
        Assert.True(updated.IsUsed);
    }

    // Le client renvoie la ressource complète sans raisonner sur le gel : des valeurs identiques
    // à celles en base ne sont pas une modification.
    [Fact]
    public async Task UpdateAsync_OnUsedProduct_WithUnchangedIdentity_Succeeds()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisson", SaleMode = SaleMode.ByWeight });
        await AddBatchAsync(dbContext, created.Id);

        var updated = await service.UpdateAsync(created.Id, Update(created, code: "sc"));

        Assert.Equal("SC", updated.Code);
    }

    // --- État dérivé (FR-001, FR-002) ------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_WithoutBatch_ReportsNotUsedAndNoRemainingStock()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisson", SaleMode = SaleMode.ByWeight });

        var result = await service.GetByIdAsync(created.Id);

        Assert.False(result.IsUsed);
        Assert.Equal(0, result.RemainingStockUnitCount);
    }

    [Fact]
    public async Task GetByIdAsync_WithBatchAndUnits_ReportsUsedAndCountsRemainingStock()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisson", SaleMode = SaleMode.ByWeight });
        var batch = await AddBatchAsync(dbContext, created.Id);
        dbContext.StockUnits.AddRange(
            new StockUnit { UnitNumber = TestUnitNumber.Next(), BatchId = batch.Id, Weight = 1m, Status = StockUnitStatus.Available },
            new StockUnit { UnitNumber = TestUnitNumber.Next(), BatchId = batch.Id, Weight = 1m, Status = StockUnitStatus.Opened },
            new StockUnit { UnitNumber = TestUnitNumber.Next(), BatchId = batch.Id, Weight = 1m, Status = StockUnitStatus.Sold },
            new StockUnit { UnitNumber = TestUnitNumber.Next(), BatchId = batch.Id, Weight = 1m, Status = StockUnitStatus.Lost });
        await dbContext.SaveChangesAsync();

        var result = await service.GetByIdAsync(created.Id);

        Assert.True(result.IsUsed);
        Assert.Equal(2, result.RemainingStockUnitCount);
    }

    [Fact]
    public async Task DeactivateAsync_SetsInactive_EvenWithExistingBatches()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisse curry", SaleMode = SaleMode.ByWeight });

        dbContext.ProductionBatches.Add(new ProductionBatch
        {
            ProductId = created.Id,
            ProductionDate = DateOnly.FromDateTime(DateTime.UtcNow),
            SalePrice = 12.5m,
        });
        await dbContext.SaveChangesAsync();

        await service.DeactivateAsync(created.Id);

        var result = await service.GetByIdAsync(created.Id);
        Assert.False(result.IsActive);
    }

    private static UpdateProductRequest Update(
        ProductDto product,
        string? name = null,
        string? code = null,
        SaleMode? saleMode = null,
        bool? allowPartialSale = null) =>
        new()
        {
            Name = name ?? product.Name,
            Code = code ?? product.Code,
            SaleMode = saleMode ?? product.SaleMode,
            AllowPartialSale = allowPartialSale ?? product.AllowPartialSale,
        };

    private static async Task<ProductionBatch> AddBatchAsync(
        Butcher.Api.Infrastructure.Data.AppDbContext dbContext, int productId, string batchNumber = "SC-260101-1")
    {
        var batch = new ProductionBatch
        {
            ProductId = productId,
            ProductionDate = new DateOnly(2026, 1, 1),
            SalePrice = 12.5m,
        };

        dbContext.ProductionBatches.Add(batch);
        await dbContext.SaveChangesAsync();
        return batch;
    }

    [Fact]
    public async Task ReactivateAsync_SetsActive()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisse curry", SaleMode = SaleMode.ByWeight });
        await service.DeactivateAsync(created.Id);

        await service.ReactivateAsync(created.Id);

        var result = await service.GetByIdAsync(created.Id);
        Assert.True(result.IsActive);
    }

    // --- Désactivation et solde des unités restantes (FR-016 à FR-021) ---------------------------

    [Fact]
    public async Task DeactivateAsync_WithRemainingStock_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisson", SaleMode = SaleMode.ByWeight });
        var batch = await AddBatchAsync(dbContext, created.Id);
        dbContext.StockUnits.Add(new StockUnit { UnitNumber = TestUnitNumber.Next(), BatchId = batch.Id, Weight = 1m, Status = StockUnitStatus.Available });
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() => service.DeactivateAsync(created.Id));

        Assert.True((await service.GetByIdAsync(created.Id)).IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_OnceEveryUnitIsOut_Succeeds()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisson", SaleMode = SaleMode.ByWeight });
        var batch = await AddBatchAsync(dbContext, created.Id);
        dbContext.StockUnits.Add(new StockUnit { UnitNumber = TestUnitNumber.Next(), BatchId = batch.Id, Weight = 1m, Status = StockUnitStatus.Sold });
        await dbContext.SaveChangesAsync();

        await service.DeactivateAsync(created.Id);

        Assert.False((await service.GetByIdAsync(created.Id)).IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_OnAlreadyInactiveProduct_IsIdempotent()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisson", SaleMode = SaleMode.ByWeight });
        await service.DeactivateAsync(created.Id);

        await service.DeactivateAsync(created.Id);

        Assert.False((await service.GetByIdAsync(created.Id)).IsActive);
    }

    [Fact]
    public async Task WriteOffStockAsync_OnAvailableUnit_RecordsLossWithWeighedWeight()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisson", SaleMode = SaleMode.ByWeight });
        var batch = await AddBatchAsync(dbContext, created.Id);
        var unit = new StockUnit { UnitNumber = TestUnitNumber.Next(), BatchId = batch.Id, Weight = 1.250m, Status = StockUnitStatus.Available };
        dbContext.StockUnits.Add(unit);
        await dbContext.SaveChangesAsync();

        var result = await service.WriteOffStockAsync(created.Id, new WriteOffProductStockRequest { StockUnitIds = [unit.Id] });

        Assert.Equal(1, result.WrittenOffCount);
        var movement = await dbContext.StockMovements.SingleAsync(m => m.StockUnitId == unit.Id);
        Assert.Equal(MovementType.Loss, movement.Type);
        Assert.Equal(1.250m, movement.SoldWeight);
        Assert.Null(movement.Amount);
        Assert.Null(movement.SaleId);
        Assert.Equal(StockUnitStatus.Lost, (await dbContext.StockUnits.FindAsync(unit.Id))!.Status);
    }

    [Fact]
    public async Task WriteOffStockAsync_OnOpenedUnit_RecordsLossWithRemainingWeightOnly()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisson", SaleMode = SaleMode.ByWeight });
        var batch = await AddBatchAsync(dbContext, created.Id);
        var unit = new StockUnit { UnitNumber = TestUnitNumber.Next(), BatchId = batch.Id, Weight = 5m, Status = StockUnitStatus.Opened };
        dbContext.StockUnits.Add(unit);
        await dbContext.SaveChangesAsync();
        dbContext.StockMovements.Add(new StockMovement
        {
            StockUnitId = unit.Id,
            Type = MovementType.Sale,
            Date = DateTimeOffset.UtcNow,
            SoldWeight = 2m,
            Amount = 30m,
        });
        await dbContext.SaveChangesAsync();

        await service.WriteOffStockAsync(created.Id, new WriteOffProductStockRequest { StockUnitIds = [unit.Id] });

        var loss = await dbContext.StockMovements.SingleAsync(m => m.StockUnitId == unit.Id && m.Type == MovementType.Loss);
        Assert.Equal(3m, loss.SoldWeight);
    }

    // Restant nul : l'unité a été vendue en totalité, la clôturer plutôt qu'enregistrer une perte
    // de poids nul, que la validation des mouvements rejetterait.
    [Fact]
    public async Task WriteOffStockAsync_OnFullySoldOpenedUnit_ClosesItInsteadOfRecordingALoss()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisson", SaleMode = SaleMode.ByWeight });
        var batch = await AddBatchAsync(dbContext, created.Id);
        var unit = new StockUnit { UnitNumber = TestUnitNumber.Next(), BatchId = batch.Id, Weight = 4m, Status = StockUnitStatus.Opened };
        dbContext.StockUnits.Add(unit);
        await dbContext.SaveChangesAsync();
        dbContext.StockMovements.Add(new StockMovement
        {
            StockUnitId = unit.Id,
            Type = MovementType.Sale,
            Date = DateTimeOffset.UtcNow,
            SoldWeight = 4m,
            Amount = 60m,
        });
        await dbContext.SaveChangesAsync();

        await service.WriteOffStockAsync(created.Id, new WriteOffProductStockRequest { StockUnitIds = [unit.Id] });

        Assert.Equal(StockUnitStatus.Sold, (await dbContext.StockUnits.FindAsync(unit.Id))!.Status);
        Assert.False(await dbContext.StockMovements.AnyAsync(m => m.StockUnitId == unit.Id && m.Type == MovementType.Loss));
        await service.DeactivateAsync(created.Id);
    }

    [Fact]
    public async Task WriteOffStockAsync_OnUnitWithoutWeight_RecordsLossWithoutWeight()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "TR", Name = "Terrine", SaleMode = SaleMode.ByPiece });
        var batch = await AddBatchAsync(dbContext, created.Id, "TR-260101-1");
        var unit = new StockUnit { UnitNumber = TestUnitNumber.Next(), BatchId = batch.Id, Weight = null, Status = StockUnitStatus.Available };
        dbContext.StockUnits.Add(unit);
        await dbContext.SaveChangesAsync();

        await service.WriteOffStockAsync(created.Id, new WriteOffProductStockRequest { StockUnitIds = [unit.Id] });

        var movement = await dbContext.StockMovements.SingleAsync(m => m.StockUnitId == unit.Id);
        Assert.Null(movement.SoldWeight);
    }

    // Sélection partielle : les unités non sélectionnées restent en stock et continuent de bloquer
    // la désactivation (FR-018).
    [Fact]
    public async Task WriteOffStockAsync_WithPartialSelection_LeavesTheRestAndKeepsDeactivationBlocked()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisson", SaleMode = SaleMode.ByWeight });
        var batch = await AddBatchAsync(dbContext, created.Id);
        var units = new[]
        {
            new StockUnit { UnitNumber = TestUnitNumber.Next(), BatchId = batch.Id, Weight = 1m, Status = StockUnitStatus.Available },
            new StockUnit { UnitNumber = TestUnitNumber.Next(), BatchId = batch.Id, Weight = 1m, Status = StockUnitStatus.Available },
            new StockUnit { UnitNumber = TestUnitNumber.Next(), BatchId = batch.Id, Weight = 1m, Status = StockUnitStatus.Available },
        };
        dbContext.StockUnits.AddRange(units);
        await dbContext.SaveChangesAsync();

        var result = await service.WriteOffStockAsync(
            created.Id, new WriteOffProductStockRequest { StockUnitIds = [units[0].Id, units[1].Id] });

        Assert.Equal(2, result.WrittenOffCount);
        Assert.Equal(1, (await service.GetByIdAsync(created.Id)).RemainingStockUnitCount);
        await Assert.ThrowsAsync<ConflictException>(() => service.DeactivateAsync(created.Id));
    }

    [Fact]
    public async Task WriteOffStockAsync_WithEmptySelection_DoesNothing()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisson", SaleMode = SaleMode.ByWeight });

        var result = await service.WriteOffStockAsync(created.Id, new WriteOffProductStockRequest { StockUnitIds = [] });

        Assert.Equal(0, result.WrittenOffCount);
    }

    [Fact]
    public async Task WriteOffStockAsync_WithAlreadyOutUnit_IgnoresItWithoutFailing()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var created = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisson", SaleMode = SaleMode.ByWeight });
        var batch = await AddBatchAsync(dbContext, created.Id);
        var sold = new StockUnit { UnitNumber = TestUnitNumber.Next(), BatchId = batch.Id, Weight = 1m, Status = StockUnitStatus.Sold };
        var available = new StockUnit { UnitNumber = TestUnitNumber.Next(), BatchId = batch.Id, Weight = 1m, Status = StockUnitStatus.Available };
        dbContext.StockUnits.AddRange(sold, available);
        await dbContext.SaveChangesAsync();

        var result = await service.WriteOffStockAsync(
            created.Id, new WriteOffProductStockRequest { StockUnitIds = [sold.Id, available.Id] });

        Assert.Equal(1, result.WrittenOffCount);
    }

    [Fact]
    public async Task WriteOffStockAsync_WithUnitOfAnotherProduct_ThrowsBadRequestException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new ProductService(dbContext);
        var target = await service.CreateAsync(new CreateProductRequest { Code = "SC", Name = "Saucisson", SaleMode = SaleMode.ByWeight });
        var other = await service.CreateAsync(new CreateProductRequest { Code = "JB", Name = "Jambon", SaleMode = SaleMode.ByWeight });
        var otherBatch = await AddBatchAsync(dbContext, other.Id, "JB-260101-1");
        var otherUnit = new StockUnit { UnitNumber = TestUnitNumber.Next(), BatchId = otherBatch.Id, Weight = 1m, Status = StockUnitStatus.Available };
        dbContext.StockUnits.Add(otherUnit);
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.WriteOffStockAsync(target.Id, new WriteOffProductStockRequest { StockUnitIds = [otherUnit.Id] }));
    }
}
