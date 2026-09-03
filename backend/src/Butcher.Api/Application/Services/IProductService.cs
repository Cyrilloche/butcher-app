using Butcher.Api.Application.Dtos;

namespace Butcher.Api.Application.Services;

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync(bool includeInactive);

    Task<ProductDto> GetByIdAsync(int id);

    Task<ProductDto> CreateAsync(CreateProductRequest request);

    Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request);

    Task DeactivateAsync(int id);

    Task ReactivateAsync(int id);
}
