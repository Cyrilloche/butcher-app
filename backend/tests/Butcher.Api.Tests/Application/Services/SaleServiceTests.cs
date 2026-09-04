using Butcher.Api.Application.Dtos;
using Butcher.Api.Application.Services;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Data;
using Butcher.Api.Tests.Support;

namespace Butcher.Api.Tests.Application.Services;

[Collection(DatabaseCollection.Name)]
public class SaleServiceTests(PostgresDatabaseFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<List<StockUnit>> SeedStockUnitsAsync(
        AppDbContext dbContext, SaleMode saleMode, int count, decimal? weight, string code = "SC")
    {
        var unitOfMeasure = new UnitOfMeasure { Label = $"kilogramme-{code}", Abbreviation = $"kg-{code}" };
        dbContext.UnitsOfMeasure.Add(unitOfMeasure);
        await dbContext.SaveChangesAsync();

        var product = new Product { Code = code, Name = "Saucisse curry", SaleMode = saleMode, SaleUnitId = unitOfMeasure.Id };
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        var batch = new ProductionBatch
        {
            BatchNumber = $"{code}-260831-1",
            ProductId = product.Id,
            ProductionDate = new DateOnly(2026, 8, 31),
            SalePrice = 12.5m,
        };
        dbContext.ProductionBatches.Add(batch);
        await dbContext.SaveChangesAsync();

        var units = Enumerable.Range(0, count)
            .Select(_ => new StockUnit { BatchId = batch.Id, Weight = weight })
            .ToList();
        dbContext.StockUnits.AddRange(units);
        await dbContext.SaveChangesAsync();
        return units;
    }

    private static async Task<Customer> SeedCustomerAsync(AppDbContext dbContext, string lastName = "Dupont")
    {
        var customer = new Customer { LastName = lastName, FirstName = "Jean" };
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();
        return customer;
    }

    // Q-05 : une vente regroupe plusieurs unités physiques sous un numéro, une date, un client et un
    // statut de paiement uniques.
    [Fact]
    public async Task CreateAsync_WithSeveralLines_GroupsThemUnderOneSale()
    {
        await using var dbContext = fixture.CreateDbContext();
        var units = await SeedStockUnitsAsync(dbContext, SaleMode.ByWeight, count: 2, weight: 0.320m);
        var customer = await SeedCustomerAsync(dbContext);
        var service = new SaleService(dbContext);

        var sale = await service.CreateAsync(new CreateSaleRequest
        {
            CustomerId = customer.Id,
            Paid = true,
            Lines =
            [
                new CreateSaleLineRequest { StockUnitId = units[0].Id, SoldWeight = 0.320m, Amount = 4m },
                new CreateSaleLineRequest { StockUnitId = units[1].Id, SoldWeight = 0.280m, Amount = 3.5m },
            ],
        });

        Assert.StartsWith("V-", sale.SaleNumber);
        Assert.Equal(customer.Id, sale.CustomerId);
        Assert.Equal("Jean Dupont", sale.CustomerName);
        Assert.True(sale.Paid);
        Assert.Equal(2, sale.ItemCount);
        Assert.Equal(7.5m, sale.Total);
        Assert.Equal(2, sale.Lines.Count);
        Assert.All(sale.Lines, line => Assert.Equal(sale.Id, line.SaleId));

        var refreshed = await dbContext.StockUnits.FindAsync(units[0].Id);
        Assert.Equal(StockUnitStatus.Sold, refreshed!.Status);
    }

    // Format `V-YYMMDD-N`, séquence remise à zéro chaque jour (même logique que le numéro de lot).
    [Fact]
    public async Task CreateAsync_SameDay_IncrementsSaleNumberSequence()
    {
        await using var dbContext = fixture.CreateDbContext();
        var units = await SeedStockUnitsAsync(dbContext, SaleMode.ByPiece, count: 2, weight: null);
        var customer = await SeedCustomerAsync(dbContext);
        var service = new SaleService(dbContext);
        var date = new DateTimeOffset(2026, 9, 4, 10, 0, 0, TimeSpan.Zero);

        var first = await service.CreateAsync(new CreateSaleRequest
        {
            CustomerId = customer.Id,
            Date = date,
            Lines = [new CreateSaleLineRequest { StockUnitId = units[0].Id, Amount = 5m }],
        });
        var second = await service.CreateAsync(new CreateSaleRequest
        {
            CustomerId = customer.Id,
            Date = date,
            Lines = [new CreateSaleLineRequest { StockUnitId = units[1].Id, Amount = 5m }],
        });

        Assert.Equal("V-260904-1", first.SaleNumber);
        Assert.Equal("V-260904-2", second.SaleNumber);
    }

    [Fact]
    public async Task CreateAsync_PartialLine_LeavesUnitOpened()
    {
        await using var dbContext = fixture.CreateDbContext();
        var units = await SeedStockUnitsAsync(dbContext, SaleMode.ByWeight, count: 1, weight: null);
        var customer = await SeedCustomerAsync(dbContext);
        var service = new SaleService(dbContext);

        await service.CreateAsync(new CreateSaleRequest
        {
            CustomerId = customer.Id,
            Lines = [new CreateSaleLineRequest { StockUnitId = units[0].Id, IsFullSale = false, SoldWeight = 0.150m, Amount = 2m }],
        });

        var refreshed = await dbContext.StockUnits.FindAsync(units[0].Id);
        Assert.Equal(StockUnitStatus.Opened, refreshed!.Status);
    }

    // La vente est atomique : une ligne invalide n'en laisse aucune enregistrée, et aucun statut
    // d'unité modifié.
    [Fact]
    public async Task CreateAsync_WithOneInvalidLine_WritesNothing()
    {
        await using var dbContext = fixture.CreateDbContext();
        var units = await SeedStockUnitsAsync(dbContext, SaleMode.ByWeight, count: 2, weight: 0.320m);
        var customer = await SeedCustomerAsync(dbContext);
        var service = new SaleService(dbContext);

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(new CreateSaleRequest
        {
            CustomerId = customer.Id,
            Lines =
            [
                new CreateSaleLineRequest { StockUnitId = units[0].Id, SoldWeight = 0.320m, Amount = 4m },
                // SoldWeight manquant sur un produit vendu au poids
                new CreateSaleLineRequest { StockUnitId = units[1].Id, Amount = 3.5m },
            ],
        }));

        Assert.Empty(dbContext.Sales);
        Assert.Empty(dbContext.StockMovements);
        var refreshed = await dbContext.StockUnits.FindAsync(units[0].Id);
        Assert.Equal(StockUnitStatus.Available, refreshed!.Status);
    }

    [Fact]
    public async Task CreateAsync_WithoutLines_ThrowsBadRequestException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var customer = await SeedCustomerAsync(dbContext);
        var service = new SaleService(dbContext);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.CreateAsync(new CreateSaleRequest { CustomerId = customer.Id, Lines = [] }));
    }

    [Fact]
    public async Task CreateAsync_WithSameUnitTwice_ThrowsBadRequestException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var units = await SeedStockUnitsAsync(dbContext, SaleMode.ByPiece, count: 1, weight: null);
        var customer = await SeedCustomerAsync(dbContext);
        var service = new SaleService(dbContext);

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(new CreateSaleRequest
        {
            CustomerId = customer.Id,
            Lines =
            [
                new CreateSaleLineRequest { StockUnitId = units[0].Id, Amount = 5m },
                new CreateSaleLineRequest { StockUnitId = units[0].Id, Amount = 5m },
            ],
        }));
    }

    [Fact]
    public async Task CreateAsync_WithUnknownCustomer_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var units = await SeedStockUnitsAsync(dbContext, SaleMode.ByPiece, count: 1, weight: null);
        var service = new SaleService(dbContext);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(new CreateSaleRequest
        {
            CustomerId = 999,
            Lines = [new CreateSaleLineRequest { StockUnitId = units[0].Id, Amount = 5m }],
        }));
    }

    [Fact]
    public async Task CreateAsync_OnAlreadySoldUnit_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var units = await SeedStockUnitsAsync(dbContext, SaleMode.ByPiece, count: 1, weight: null);
        var customer = await SeedCustomerAsync(dbContext);
        var service = new SaleService(dbContext);
        await service.CreateAsync(new CreateSaleRequest
        {
            CustomerId = customer.Id,
            Lines = [new CreateSaleLineRequest { StockUnitId = units[0].Id, Amount = 5m }],
        });

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(new CreateSaleRequest
        {
            CustomerId = customer.Id,
            Lines = [new CreateSaleLineRequest { StockUnitId = units[0].Id, Amount = 5m }],
        }));
    }

    // Q-04 : statut de paiement, basculable en un geste depuis la liste des ventes.
    [Fact]
    public async Task SetPaymentAsync_TogglesPaidFlag()
    {
        await using var dbContext = fixture.CreateDbContext();
        var units = await SeedStockUnitsAsync(dbContext, SaleMode.ByPiece, count: 1, weight: null);
        var customer = await SeedCustomerAsync(dbContext);
        var service = new SaleService(dbContext);
        var sale = await service.CreateAsync(new CreateSaleRequest
        {
            CustomerId = customer.Id,
            Paid = false,
            Lines = [new CreateSaleLineRequest { StockUnitId = units[0].Id, Amount = 5m }],
        });

        var updated = await service.SetPaymentAsync(sale.Id, new SetSalePaymentRequest { Paid = true });

        Assert.True(updated.Paid);
    }

    [Fact]
    public async Task UpdateAsync_ChangesCustomerAndNotes()
    {
        await using var dbContext = fixture.CreateDbContext();
        var units = await SeedStockUnitsAsync(dbContext, SaleMode.ByPiece, count: 1, weight: null);
        var customer = await SeedCustomerAsync(dbContext);
        var other = await SeedCustomerAsync(dbContext, lastName: "Martin");
        var service = new SaleService(dbContext);
        var sale = await service.CreateAsync(new CreateSaleRequest
        {
            CustomerId = customer.Id,
            Lines = [new CreateSaleLineRequest { StockUnitId = units[0].Id, Amount = 5m }],
        });

        var updated = await service.UpdateAsync(sale.Id, new UpdateSaleRequest
        {
            CustomerId = other.Id,
            Date = sale.Date,
            Paid = true,
            Notes = "Réglée le lendemain",
        });

        Assert.Equal(other.Id, updated.CustomerId);
        Assert.Equal("Jean Martin", updated.CustomerName);
        Assert.Equal("Réglée le lendemain", updated.Notes);
    }

    // RG-11 : une vente reste supprimable ; les unités qui n'ont plus aucun mouvement redeviennent
    // disponibles.
    [Fact]
    public async Task DeleteAsync_RevertsUnitsToAvailable()
    {
        await using var dbContext = fixture.CreateDbContext();
        var units = await SeedStockUnitsAsync(dbContext, SaleMode.ByPiece, count: 2, weight: null);
        var customer = await SeedCustomerAsync(dbContext);
        var service = new SaleService(dbContext);
        var sale = await service.CreateAsync(new CreateSaleRequest
        {
            CustomerId = customer.Id,
            Lines =
            [
                new CreateSaleLineRequest { StockUnitId = units[0].Id, Amount = 5m },
                new CreateSaleLineRequest { StockUnitId = units[1].Id, Amount = 6m },
            ],
        });

        await service.DeleteAsync(sale.Id);

        Assert.Empty(dbContext.Sales);
        Assert.Empty(dbContext.StockMovements);
        foreach (var unit in units)
        {
            var refreshed = await dbContext.StockUnits.FindAsync(unit.Id);
            Assert.Equal(StockUnitStatus.Available, refreshed!.Status);
        }
    }

    [Fact]
    public async Task GetAllAsync_FilteredByPaid_ReturnsOnlyMatchingSales()
    {
        await using var dbContext = fixture.CreateDbContext();
        var units = await SeedStockUnitsAsync(dbContext, SaleMode.ByPiece, count: 2, weight: null);
        var customer = await SeedCustomerAsync(dbContext);
        var service = new SaleService(dbContext);
        await service.CreateAsync(new CreateSaleRequest
        {
            CustomerId = customer.Id,
            Paid = true,
            Lines = [new CreateSaleLineRequest { StockUnitId = units[0].Id, Amount = 5m }],
        });
        var unpaid = await service.CreateAsync(new CreateSaleRequest
        {
            CustomerId = customer.Id,
            Paid = false,
            Lines = [new CreateSaleLineRequest { StockUnitId = units[1].Id, Amount = 6m }],
        });

        var result = await service.GetAllAsync(customerId: null, paid: false, from: null, to: null);

        Assert.Single(result);
        Assert.Equal(unpaid.Id, result[0].Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ThrowsNotFoundException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new SaleService(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(999));
    }
}
