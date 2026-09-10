using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Domain.Entities;

public class StockUnit
{
    public int Id { get; set; }

    public int BatchId { get; set; }

    public ProductionBatch? Batch { get; set; }

    /// <summary>
    /// Le numéro recopié à la main sur l'étiquette de cet objet, au format CODE-YYMMDD-N.
    /// </summary>
    /// <remarks>
    /// Écrit une fois, à la création de l'unité, et jamais modifié ensuite (FR-006) : l'étiquette
    /// physique, elle, ne se réécrit pas. C'est l'identité de l'objet aux yeux de l'utilisateur.
    /// </remarks>
    public required string UnitNumber { get; set; }

    public decimal? Weight { get; set; }

    public StockUnitStatus Status { get; set; } = StockUnitStatus.Available;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<StockMovement> StockMovements { get; set; } = [];
}
