namespace Butcher.Api.Domain.Entities;

/// <summary>
/// Registre des séquences de numéro de lot déjà émises, pour un produit et une date de production.
/// </summary>
/// <remarks>
/// Son unique raison d'être est qu'un numéro de lot émis ne soit jamais réattribué, y compris après
/// la suppression du lot qui le portait (FR-013). La numérotation ne peut donc pas être dérivée du
/// nombre de lots existants : supprimer l'unique lot du jour ferait retomber le compte à zéro et
/// réémettrait le même numéro sur une seconde série d'étiquettes manuscrites, indiscernable de la
/// première.
///
/// Conséquence assumée : les séries de numéros comportent des trous après une suppression. Ces
/// lignes ne sont jamais supprimées, c'est précisément ce qui garantit la propriété.
/// </remarks>
public class BatchNumberSequence
{
    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public DateOnly ProductionDate { get; set; }

    public int LastSequence { get; set; }
}
