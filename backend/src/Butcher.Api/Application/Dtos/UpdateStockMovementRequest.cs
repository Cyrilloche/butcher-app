using System.ComponentModel.DataAnnotations;

namespace Butcher.Api.Application.Dtos;

public class UpdateStockMovementRequest
{
    public decimal? SoldWeight { get; set; }

    public decimal? Amount { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}
