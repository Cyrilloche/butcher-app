using Butcher.Api.Application.Dtos;

namespace Butcher.Api.Application.Services;

public interface ICustomerService
{
    Task<List<CustomerDto>> GetAllAsync();

    Task<CustomerDto> GetByIdAsync(int id);

    Task<CustomerDto> CreateAsync(CreateCustomerRequest request);

    Task<CustomerDto> UpdateAsync(int id, UpdateCustomerRequest request);

    Task DeleteAsync(int id);
}
