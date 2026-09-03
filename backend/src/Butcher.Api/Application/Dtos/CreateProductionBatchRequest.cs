using System.ComponentModel.DataAnnotations;

namespace Butcher.Api.Application.Dtos;

public class CreateProductionBatchRequest
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    public DateOnly ProductionDate { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal SalePrice { get; set; }

    [MaxLength(500)]
    public string? RawMaterialRef { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}
