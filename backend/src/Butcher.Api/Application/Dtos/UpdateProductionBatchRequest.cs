using System.ComponentModel.DataAnnotations;

namespace Butcher.Api.Application.Dtos;

public class UpdateProductionBatchRequest
{
    [Range(0.01, double.MaxValue)]
    public decimal SalePrice { get; set; }

    [MaxLength(500)]
    public string? RawMaterialRef { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}
