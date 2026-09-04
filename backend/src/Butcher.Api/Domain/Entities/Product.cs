using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Domain.Entities;

public class Product
{
    public int Id { get; set; }

    public required string Code { get; set; }

    public required string Name { get; set; }

    public SaleMode SaleMode { get; set; }

    public bool AllowPartialSale { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<ProductionBatch> ProductionBatches { get; set; } = [];
}
