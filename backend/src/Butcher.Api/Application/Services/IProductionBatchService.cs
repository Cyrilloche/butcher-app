using Butcher.Api.Application.Dtos;

namespace Butcher.Api.Application.Services;

public interface IProductionBatchService
{
    Task<List<ProductionBatchDto>> GetAllAsync(int? productId);

    Task<ProductionBatchDto> GetByIdAsync(int id);

    Task<ProductionBatchDto> CreateAsync(CreateProductionBatchRequest request);

    Task<ProductionBatchDto> UpdateAsync(int id, UpdateProductionBatchRequest request);
}
