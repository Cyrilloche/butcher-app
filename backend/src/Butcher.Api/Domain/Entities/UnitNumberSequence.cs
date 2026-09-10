namespace Butcher.Api.Domain.Entities;

/// <summary>
/// Registre des rangs de numéro d'unité déjà émis, pour un produit et une date de production.
/// </summary>
/// <remarks>
/// Son unique raison d'être est qu'un numéro d'étiquette émis ne soit jamais réattribué, y compris
/// après la suppression de l'unité qui le portait ou de la fournée dont elle provenait (FR-004).
/// La numérotation ne peut donc pas être dérivée d'un comptage des unités existantes : supprimer
/// les unités du jour ferait retomber le compte à zéro et réémettrait les mêmes numéros sur une
/// seconde série d'étiquettes manuscrites, indiscernable de la première.
///
/// Conséquence assumée : les séries comportent des trous après une suppression. Ces lignes ne sont
/// jamais supprimées, c'est précisément ce qui garantit la propriété.
/// </remarks>
public class UnitNumberSequence
{
    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public DateOnly ProductionDate { get; set; }

    /// <summary>Dernier rang d'unité émis pour ce couple produit / date de production.</summary>
    public int LastSequence { get; set; }
}
