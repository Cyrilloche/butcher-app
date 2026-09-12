namespace Butcher.Api.Application.Dtos;

public class ProductionBatchDto
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public required string ProductName { get; set; }

    public DateOnly ProductionDate { get; set; }

    public decimal SalePrice { get; set; }

    public string? RawMaterialRef { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Nom affiché du compte qui a enregistré la fabrication, ou <c>null</c> pour une fabrication
    /// antérieure aux comptes nominatifs (RF-27, FR-020a).
    /// </summary>
    public string? CreatedByName { get; set; }
}
