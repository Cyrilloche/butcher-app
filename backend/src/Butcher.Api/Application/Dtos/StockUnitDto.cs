using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Application.Dtos;

public class StockUnitDto
{
    public int Id { get; set; }

    public int BatchId { get; set; }

    /// <summary>Le numéro écrit sur l'étiquette de cet objet, au format CODE-YYMMDD-N.</summary>
    public required string UnitNumber { get; set; }

    public decimal? Weight { get; set; }

    public StockUnitStatus Status { get; set; }
}
