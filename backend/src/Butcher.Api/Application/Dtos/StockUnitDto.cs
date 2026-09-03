using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Application.Dtos;

public class StockUnitDto
{
    public int Id { get; set; }

    public int BatchId { get; set; }

    public required string BatchNumber { get; set; }

    public decimal? Weight { get; set; }

    public StockUnitStatus Status { get; set; }
}
