using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Application.Assistant;

/// <summary>
/// Une unité encore en stock (<c>available</c> ou <c>opened</c>), telle que l'assistant la voit :
/// assez pour choisir les unités d'une vente et répondre à une question de stock.
/// </summary>
/// <param name="RemainingWeight">Poids encore vendable, calculé par <c>ComputeRemainingWeight</c> ; null à la pièce.</param>
/// <param name="SalePrice">Prix de la fournée : au kilo pour un produit au poids, à la pièce sinon.</param>
public sealed record SellableUnit(
    int Id,
    string UnitNumber,
    string ProductCode,
    string ProductName,
    SaleMode SaleMode,
    bool AllowPartialSale,
    int BatchId,
    DateOnly ProductionDate,
    decimal SalePrice,
    decimal? Weight,
    decimal? RemainingWeight,
    StockUnitStatus Status);
