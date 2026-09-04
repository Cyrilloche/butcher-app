using Butcher.Api.Application.Dtos;

namespace Butcher.Api.Application.Services;

public interface IStockMovementService
{
    Task<List<StockMovementDto>> GetAllAsync(int? stockUnitId, int? customerId, int? saleId);

    Task<StockMovementDto> GetByIdAsync(int id);

    Task<StockMovementDto> CreateAsync(int stockUnitId, CreateStockMovementRequest request);

    Task<StockMovementDto> UpdateAsync(int id, UpdateStockMovementRequest request);

    Task DeleteAsync(int id);

    Task CloseAsync(int stockUnitId);
}
