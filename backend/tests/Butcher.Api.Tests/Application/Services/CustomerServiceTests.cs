using Butcher.Api.Application.Dtos;
using Butcher.Api.Application.Services;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Tests.Support;

namespace Butcher.Api.Tests.Application.Services;

[Collection(DatabaseCollection.Name)]
public class CustomerServiceTests(PostgresDatabaseFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesCustomer()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new CustomerService(dbContext);

        var result = await service.CreateAsync(new CreateCustomerRequest { LastName = "Dupont", FirstName = "Jean", Phone = "0600000000" });

        Assert.True(result.Id > 0);
        Assert.Equal("Dupont", result.LastName);
        Assert.Equal("Jean", result.FirstName);
    }

    [Fact]
    public async Task CreateAsync_WithoutFirstName_Succeeds()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new CustomerService(dbContext);

        var result = await service.CreateAsync(new CreateCustomerRequest { LastName = "Dupont" });

        Assert.Null(result.FirstName);
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ThrowsNotFoundException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new CustomerService(dbContext);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(999));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsCustomersOrderedByLastName()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new CustomerService(dbContext);
        await service.CreateAsync(new CreateCustomerRequest { LastName = "Zorro" });
        await service.CreateAsync(new CreateCustomerRequest { LastName = "Anderson" });

        var result = await service.GetAllAsync();

        Assert.Equal(["Anderson", "Zorro"], result.Select(c => c.LastName));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFields()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new CustomerService(dbContext);
        var created = await service.CreateAsync(new CreateCustomerRequest { LastName = "Dupont" });

        var updated = await service.UpdateAsync(created.Id, new UpdateCustomerRequest { LastName = "Dupont", FirstName = "Jeanne", Phone = "0611111111", Notes = "Habituée" });

        Assert.Equal("Jeanne", updated.FirstName);
        Assert.Equal("0611111111", updated.Phone);
        Assert.Equal("Habituée", updated.Notes);
    }

    [Fact]
    public async Task DeleteAsync_RemovesCustomer()
    {
        await using var dbContext = fixture.CreateDbContext();
        var service = new CustomerService(dbContext);
        var created = await service.CreateAsync(new CreateCustomerRequest { LastName = "Dupont" });

        await service.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(created.Id));
    }
}
