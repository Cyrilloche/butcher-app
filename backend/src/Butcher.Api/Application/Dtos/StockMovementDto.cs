using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Application.Dtos;

public class StockMovementDto
{
    public int Id { get; set; }

    public int StockUnitId { get; set; }

    // Contexte produit de la ligne, résolu via stock_unit -> production_batch -> product : sans ça,
    // le frontend n'a d'autre choix que de deviner le produit à partir du préfixe du numéro de lot.
    public string? ProductName { get; set; }

    /// <summary>
    /// Faux si le produit a été désactivé depuis. Le mouvement reste affiché dans l'historique,
    /// simplement signalé (FR-024).
    /// </summary>
    public bool ProductIsActive { get; set; }

    /// <summary>
    /// Le numéro de l'unité sortie, tel qu'il est écrit sur son étiquette. C'est par lui que
    /// l'utilisateur reconnaît l'objet dont il est question (FR-009).
    /// </summary>
    public string? UnitNumber { get; set; }

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

    /// <summary>
    /// Nom affiché du compte qui a enregistré la sortie, ou <c>null</c> pour une sortie antérieure aux
    /// comptes nominatifs (RF-27, FR-020a).
    /// </summary>
    public string? CreatedByName { get; set; }
}
