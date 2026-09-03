using Butcher.Api.Application.Dtos;
using Butcher.Api.Common.Exceptions;
using Butcher.Api.Domain.Entities;
using Butcher.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Butcher.Api.Application.Services;

public class CustomerService(AppDbContext dbContext) : ICustomerService
{
    public async Task<List<CustomerDto>> GetAllAsync() =>
        await dbContext.Customers.OrderBy(c => c.LastName).Select(c => ToDto(c)).ToListAsync();

    public async Task<CustomerDto> GetByIdAsync(int id)
    {
        var customer = await FindOrThrowAsync(id);
        return ToDto(customer);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request)
    {
        var customer = new Customer
        {
            LastName = request.LastName,
            FirstName = request.FirstName,
            Phone = request.Phone,
            Notes = request.Notes,
        };

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();

        return ToDto(customer);
    }

    public async Task<CustomerDto> UpdateAsync(int id, UpdateCustomerRequest request)
    {
        var customer = await FindOrThrowAsync(id);

        customer.LastName = request.LastName;
        customer.FirstName = request.FirstName;
        customer.Phone = request.Phone;
        customer.Notes = request.Notes;
        await dbContext.SaveChangesAsync();

        return ToDto(customer);
    }

    public async Task DeleteAsync(int id)
    {
        var customer = await FindOrThrowAsync(id);
        dbContext.Customers.Remove(customer);
        await dbContext.SaveChangesAsync();
    }

    private async Task<Customer> FindOrThrowAsync(int id) =>
        await dbContext.Customers.FindAsync(id)
            ?? throw new NotFoundException($"Client {id} introuvable.");

    private static CustomerDto ToDto(Customer customer) =>
        new()
        {
            Id = customer.Id,
            LastName = customer.LastName,
            FirstName = customer.FirstName,
            Phone = customer.Phone,
            Notes = customer.Notes,
        };
}
