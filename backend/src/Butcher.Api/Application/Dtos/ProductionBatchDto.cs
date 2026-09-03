namespace Butcher.Api.Application.Dtos;

public class ProductionBatchDto
{
    public int Id { get; set; }

    public required string BatchNumber { get; set; }

    public int ProductId { get; set; }

    public required string ProductName { get; set; }

    public DateOnly ProductionDate { get; set; }

    public decimal SalePrice { get; set; }

    public string? RawMaterialRef { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public string? Notes { get; set; }
}
