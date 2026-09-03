namespace Butcher.Api.Domain.Entities;

public class ProductionBatch
{
    public int Id { get; set; }

    public required string BatchNumber { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public DateOnly ProductionDate { get; set; }

    public decimal SalePrice { get; set; }

    public string? RawMaterialRef { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public string? Notes { get; set; }

    public Guid? CreatedById { get; set; }

    public AppUser? CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<StockUnit> StockUnits { get; set; } = [];
}
