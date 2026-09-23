using System.Globalization;
using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Application.Assistant;

/// <param name="RemainingKg">Poids encore vendable de la fournée ; null à la pièce.</param>
public sealed record BatchStock(DateOnly ProductionDate, decimal SalePrice, int Count, decimal? RemainingKg);

public sealed record OpenedUnitStock(string UnitNumber, DateOnly ProductionDate, decimal? RemainingKg);

/// <param name="WholeCount">Unités intactes (<c>available</c>).</param>
/// <param name="RemainingKg">Poids vendable total, entamés compris ; null à la pièce.</param>
public sealed record ProductStock(string Code, string Name, SaleMode SaleMode, int WholeCount, int OpenedCount,
    decimal? RemainingKg, DateOnly? OldestDate, IReadOnlyList<BatchStock> Batches, IReadOnlyList<OpenedUnitStock> Opened);

/// <summary>
/// Réponse à une question de stock (outil <c>get_stock</c>, RF-34, FR-010) : les chiffres viennent
/// d'ici, jamais d'un calcul du LLM. Mêmes règles que les écrans de stock : on compte les unités
/// <c>available</c> et <c>opened</c>, et le poids restant plutôt que le poids d'origine.
/// </summary>
public static class StockSummaryBuilder
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    public static IReadOnlyList<ProductStock> Build(IEnumerable<SellableUnit> stock, string? productCode) =>
        stock.Where(u => productCode is null || string.Equals(u.ProductCode, productCode, StringComparison.OrdinalIgnoreCase))
            .GroupBy(u => u.ProductCode)
            .Select(g =>
            {
                var first = g.First();
                var byWeight = first.SaleMode == SaleMode.ByWeight;
                return new ProductStock(
                    first.ProductCode, first.ProductName, first.SaleMode,
                    WholeCount: g.Count(u => u.Status == StockUnitStatus.Available),
                    OpenedCount: g.Count(u => u.Status == StockUnitStatus.Opened),
                    RemainingKg: byWeight ? g.Sum(u => u.RemainingWeight ?? 0m) : null,
                    OldestDate: g.Min(u => u.ProductionDate),
                    Batches: g.GroupBy(u => u.BatchId)
                        .Select(b => new BatchStock(b.First().ProductionDate, b.First().SalePrice, b.Count(),
                            byWeight ? b.Sum(u => u.RemainingWeight ?? 0m) : null))
                        .OrderBy(b => b.ProductionDate).ToList(),
                    Opened: g.Where(u => u.Status == StockUnitStatus.Opened)
                        .OrderBy(u => u.ProductionDate)
                        .Select(u => new OpenedUnitStock(u.UnitNumber, u.ProductionDate, u.RemainingWeight)).ToList());
            })
            .OrderBy(p => p.Name)
            .ToList();

    /// <summary>
    /// La phrase dite, construite par le backend et jamais par le LLM (FR-011) : l'essentiel en une ou
    /// deux phrases, le détail restant à l'écran.
    /// </summary>
    public static string Speech(IReadOnlyList<ProductStock> products, string? askedProductName = null)
    {
        if (products.Count == 0)
            return askedProductName is null ? "Il ne te reste rien en stock." : $"Il ne te reste plus de {askedProductName.ToLowerInvariant()}.";
        // Le nom du produit n'est jamais accordé : « saucisse curry » ne se met pas au pluriel mot à mot.
        if (products.Count > 1)
            return "Il te reste : " + string.Join(" ; ", products.Select(p => $"{p.Name.ToLowerInvariant()}, {Units(p)}")) + ".";

        var product = products[0];
        var openedLeft = product.Opened.Sum(o => o.RemainingKg ?? 0m);
        var weight = product.OpenedCount > 0 && product.WholeCount == 0 ? openedLeft : product.RemainingKg ?? 0m;
        var sentence = $"{product.Name} : il t'en reste {Units(product)}"
            + (product.OpenedCount > 0 && product.WholeCount > 0 && openedLeft > 0
                ? $", l'entamé fait encore environ {Kilos(openedLeft)}"
                : weight > 0 ? $", environ {Kilos(weight)}" : "");
        if (product.OldestDate is { } oldest && product.Batches.Count > 1)
            sentence += $". Les plus anciens datent du {oldest.ToString("d MMMM", French)}";
        return sentence + ".";
    }

    private static string Units(ProductStock p) => (p.WholeCount, p.OpenedCount) switch
    {
        (_, 0) => $"{p.WholeCount}",
        (0, var opened) => $"{opened} entamé{(opened > 1 ? "s" : "")}",
        var (whole, opened) => $"{whole} entier{(whole > 1 ? "s" : "")} et {opened} entamé{(opened > 1 ? "s" : "")}",
    };

    private static string Kilos(decimal kilograms) =>
        kilograms < 1m ? $"{Math.Round(kilograms * 1000m / 10m) * 10m:0} grammes" : $"{Math.Round(kilograms, 1).ToString("0.#", French)} kilos";
}
