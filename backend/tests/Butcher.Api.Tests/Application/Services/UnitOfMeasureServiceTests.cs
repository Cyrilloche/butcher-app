using Butcher.Api.Application.Dtos;
using Butcher.Api.Application.Services;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Tests.Support;

namespace Butcher.Api.Tests.Application.Services;

[Collection(DatabaseCollection.Name)]
public class UnitOfMeasureServiceTests(PostgresDatabaseFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesUnit()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new UnitOfMeasureService(dbContext);

        var result = await service.CreateAsync(new CreateUnitOfMeasureRequest { Label = "kilogramme", Abbreviation = "kg" });

        Assert.True(result.Id > 0);
        Assert.Equal("kilogramme", result.Label);
        Assert.Equal("kg", result.Abbreviation);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateLabel_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new UnitOfMeasureService(dbContext);
        await service.CreateAsync(new CreateUnitOfMeasureRequest { Label = "kilogramme", Abbreviation = "kg" });

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new CreateUnitOfMeasureRequest { Label = "kilogramme", Abbreviation = "autre" }));
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateAbbreviation_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new UnitOfMeasureService(dbContext);
        await service.CreateAsync(new CreateUnitOfMeasureRequest { Label = "kilogramme", Abbreviation = "kg" });

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new CreateUnitOfMeasureRequest { Label = "autre", Abbreviation = "kg" }));
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ThrowsNotFoundException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new UnitOfMeasureService(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(999));
    }

    [Fact]
    public async Task GetAllAsync_ByDefault_ReturnsOnlyActiveUnits()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new UnitOfMeasureService(dbContext);
        var active = await service.CreateAsync(new CreateUnitOfMeasureRequest { Label = "kilogramme", Abbreviation = "kg" });
        var inactive = await service.CreateAsync(new CreateUnitOfMeasureRequest { Label = "piece", Abbreviation = "pc" });
        await service.DeactivateAsync(inactive.Id);

        var result = await service.GetAllAsync(includeInactive: false);

        Assert.Single(result);
        Assert.Equal(active.Id, result[0].Id);
    }

    [Fact]
    public async Task GetAllAsync_WithIncludeInactive_ReturnsAllUnits()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new UnitOfMeasureService(dbContext);
        await service.CreateAsync(new CreateUnitOfMeasureRequest { Label = "kilogramme", Abbreviation = "kg" });
        var inactive = await service.CreateAsync(new CreateUnitOfMeasureRequest { Label = "piece", Abbreviation = "pc" });
        await service.DeactivateAsync(inactive.Id);

        var result = await service.GetAllAsync(includeInactive: true);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesLabelAndAbbreviation()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new UnitOfMeasureService(dbContext);
        var created = await service.CreateAsync(new CreateUnitOfMeasureRequest { Label = "kilogramme", Abbreviation = "kg" });

        var updated = await service.UpdateAsync(created.Id, new UpdateUnitOfMeasureRequest { Label = "kilo", Abbreviation = "kgg" });

        Assert.Equal("kilo", updated.Label);
        Assert.Equal("kgg", updated.Abbreviation);
    }

    [Fact]
    public async Task DeactivateAsync_WhenUsedByActiveProduct_ThrowsConflictException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new UnitOfMeasureService(dbContext);
        var unit = await service.CreateAsync(new CreateUnitOfMeasureRequest { Label = "kilogramme", Abbreviation = "kg" });

        dbContext.Products.Add(new Product
        {
            Code = "SC",
            Name = "Saucisse curry",
            SaleMode = SaleMode.ByWeight,
            SaleUnitId = unit.Id,
            IsActive = true,
        });
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() => service.DeactivateAsync(unit.Id));
    }

    [Fact]
    public async Task DeactivateAsync_WhenNotUsed_SetsInactive()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new UnitOfMeasureService(dbContext);
        var unit = await service.CreateAsync(new CreateUnitOfMeasureRequest { Label = "kilogramme", Abbreviation = "kg" });

        await service.DeactivateAsync(unit.Id);

        var result = await service.GetByIdAsync(unit.Id);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task ReactivateAsync_SetsActive()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new UnitOfMeasureService(dbContext);
        var unit = await service.CreateAsync(new CreateUnitOfMeasureRequest { Label = "kilogramme", Abbreviation = "kg" });
        await service.DeactivateAsync(unit.Id);

        await service.ReactivateAsync(unit.Id);

        var result = await service.GetByIdAsync(unit.Id);
        Assert.True(result.IsActive);
    }
}
