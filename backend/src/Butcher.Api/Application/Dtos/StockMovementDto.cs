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

    /// <summary>Renseigné pour les mouvements de type « vente » uniquement.</summary>
    public int? SaleId { get; set; }

    public string? SaleNumber { get; set; }

    // Lecture seule : le client est porté par la vente, pas par le mouvement. Exposé ici pour
    // éviter au frontend un aller-retour supplémentaire sur les vues « historique par unité ».
    public int? CustomerId { get; set; }

    public string? CustomerName { get; set; }

    public string? Notes { get; set; }
}
