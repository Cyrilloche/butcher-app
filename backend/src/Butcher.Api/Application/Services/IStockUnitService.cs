using Butcher.Api.Application.Dtos;
using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Application.Services;

public interface IStockUnitService
{
    Task<List<StockUnitDto>> GetAllAsync(int? batchId, StockUnitStatus? status);

    Task<StockUnitDto> GetByIdAsync(int id);

    Task<List<StockUnitDto>> AddUnitsAsync(int batchId, AddStockUnitsRequest request);

    Task DeleteAsync(int id);
}
