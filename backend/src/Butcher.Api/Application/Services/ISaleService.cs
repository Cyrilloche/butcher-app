using Butcher.Api.Application.Dtos;

namespace Butcher.Api.Application.Services;

public interface ISaleService
{
    Task<List<SaleDto>> GetAllAsync(int? customerId, bool? paid, DateTimeOffset? from, DateTimeOffset? to);

    Task<SaleDto> GetByIdAsync(int id);

    Task<SaleDto> CreateAsync(CreateSaleRequest request);

    Task<SaleDto> UpdateAsync(int id, UpdateSaleRequest request);

    Task<SaleDto> SetPaymentAsync(int id, SetSalePaymentRequest request);

    Task DeleteAsync(int id);
}
