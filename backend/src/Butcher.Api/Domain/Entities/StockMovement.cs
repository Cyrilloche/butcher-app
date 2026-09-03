using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Domain.Entities;

public class StockMovement
{
    public int Id { get; set; }

    public int StockUnitId { get; set; }

    public StockUnit? StockUnit { get; set; }

    public MovementType Type { get; set; }

    public DateTimeOffset Date { get; set; }

    public decimal? SoldWeight { get; set; }

    public decimal? Amount { get; set; }

    public int? CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public string? Notes { get; set; }

    public Guid? CreatedById { get; set; }

    public AppUser? CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
