using Butcher.Api.Application.Dtos;

namespace Butcher.Api.Application.Services;

public interface IUnitOfMeasureService
{
    Task<List<UnitOfMeasureDto>> GetAllAsync(bool includeInactive);

    Task<UnitOfMeasureDto> GetByIdAsync(int id);

    Task<UnitOfMeasureDto> CreateAsync(CreateUnitOfMeasureRequest request);

    Task<UnitOfMeasureDto> UpdateAsync(int id, UpdateUnitOfMeasureRequest request);

    Task DeactivateAsync(int id);

    Task ReactivateAsync(int id);
}
