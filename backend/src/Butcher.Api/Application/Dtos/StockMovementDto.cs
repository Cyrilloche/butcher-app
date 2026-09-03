using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Application.Dtos;

public class StockMovementDto
{
    public int Id { get; set; }

    public int StockUnitId { get; set; }

    public MovementType Type { get; set; }

    public DateTimeOffset Date { get; set; }

    public decimal? SoldWeight { get; set; }

    public decimal? Amount { get; set; }

    public int? CustomerId { get; set; }

    public string? CustomerName { get; set; }

    public string? Notes { get; set; }
}
