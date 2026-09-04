using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Application.Dtos;

public class StockMovementDto
{
    public int Id { get; set; }

    public int StockUnitId { get; set; }

    // Contexte produit de la ligne, résolu via stock_unit -> production_batch -> product : sans ça,
    // le frontend n'a d'autre choix que de deviner le produit à partir du préfixe du numéro de lot.
    public string? ProductName { get; set; }

    public string? BatchNumber { get; set; }

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
