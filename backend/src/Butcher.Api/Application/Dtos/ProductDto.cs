using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Application.Dtos;

public class ProductDto
{
    public int Id { get; set; }

    public required string Code { get; set; }

    public required string Name { get; set; }

    public SaleMode SaleMode { get; set; }

    public bool AllowPartialSale { get; set; }

    public bool IsActive { get; set; }

    /// <summary>
    /// Vrai dès qu'au moins un lot de production est rattaché au produit. Décrit un fait, pas une
    /// politique : c'est l'interface qui en déduit les champs à présenter en lecture seule (FR-001,
    /// FR-002). État dérivé, jamais stocké.
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Nombre d'unités de stock encore « available » ou « opened ». Pilote le garde-fou de
    /// désactivation (FR-016) et alimente l'écran de solde.
    /// </summary>
    public int RemainingStockUnitCount { get; set; }
}
