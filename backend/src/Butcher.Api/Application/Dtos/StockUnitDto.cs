using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Application.Dtos;

public class StockUnitDto
{
    public int Id { get; set; }

    public int BatchId { get; set; }

    /// <summary>Le numéro écrit sur l'étiquette de cet objet, au format CODE-YYMMDD-N.</summary>
    public required string UnitNumber { get; set; }

    /// <summary>Poids pesé à la fabrication. Ne bouge jamais.</summary>
    public decimal? Weight { get; set; }

    /// <summary>
    /// Poids encore vendable : <see cref="Weight"/> moins la somme des poids déjà vendus sur cette
    /// unité (RG-05). Égal au poids pesé sur une unité intacte, nul sur une unité entièrement
    /// vendue, <c>null</c> si l'unité n'a pas de poids.
    /// </summary>
    /// <remarks>
    /// Valeur <b>calculée à chaque lecture et jamais stockée</b> : aucune colonne, aucun cache,
    /// aucune donnée à maintenir en cohérence. C'est toute la portée de RG-05.
    /// </remarks>
    public decimal? RemainingWeight { get; set; }

    public StockUnitStatus Status { get; set; }
}
