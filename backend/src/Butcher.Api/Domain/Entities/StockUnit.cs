using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Domain.Entities;

public class StockUnit
{
    public int Id { get; set; }

    public int BatchId { get; set; }

    public ProductionBatch? Batch { get; set; }

    public decimal? Weight { get; set; }

    public StockUnitStatus Status { get; set; } = StockUnitStatus.Available;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<StockMovement> StockMovements { get; set; } = [];
}
