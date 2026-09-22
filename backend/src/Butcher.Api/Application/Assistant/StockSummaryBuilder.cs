using System.Globalization;
using System.Text.RegularExpressions;
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
/// Réponse à une question de stock (outil <c>get_stock</c>, cadrage §7) : les chiffres viennent
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
    /// Phrase construite par le backend, sans LLM : repli quand la phrase du LLM contient un chiffre
    /// absent des données, et référence de ce qu'on attend (l'essentiel en une ou deux phrases).
    /// </summary>
    public static string Speech(IReadOnlyList<ProductStock> products, string? askedProductName = null)
    {
        if (products.Count == 0)
            return askedProductName is null ? "Il ne te reste rien en stock." : $"Il ne te reste plus de {askedProductName.ToLowerInvariant()}.";
        if (products.Count > 1)
            return "Il te reste " + string.Join(", ", products.Select(p => Count(p.WholeCount + p.OpenedCount, p.Name))) + ".";

        var product = products[0];
        var openedLeft = product.Opened.Sum(o => o.RemainingKg ?? 0m);
        var openedPart = $"{product.OpenedCount} entamé{(product.OpenedCount > 1 ? "s" : "")}"
            + (openedLeft > 0 ? $" dont il reste environ {Kilos(openedLeft)}" : "");
        var sentence = product.OpenedCount == 0
            ? $"Il te reste {Count(product.WholeCount, product.Name)}"
              + (product.RemainingKg is { } kg and > 0 ? $", environ {Kilos(kg)}" : "")
            : product.WholeCount == 0
                ? $"Il te reste seulement {Count(product.OpenedCount, product.Name)} entamé{(product.OpenedCount > 1 ? "s" : "")}"
                  + (openedLeft > 0 ? $", environ {Kilos(openedLeft)}" : "")
                : $"Il te reste {Count(product.WholeCount, product.Name)} entier{(product.WholeCount > 1 ? "s" : "")}, et {openedPart}";
        if (product.OldestDate is { } oldest && product.Batches.Count > 1)
            sentence += $". Les plus anciens datent du {oldest.ToString("d MMMM", French)}";
        return sentence + ".";
    }

    /// <summary>
    /// Nombres présents dans une phrase et absents des données : vide si la phrase est fidèle.
    /// Arrondis admis : poids au kilo, au dixième, au centième ou en grammes ; dates en jour, mois, année.
    /// </summary>
    public static IReadOnlyList<string> InventedNumbers(string speech, IReadOnlyList<ProductStock> products)
    {
        var allowed = new HashSet<decimal>();
        foreach (var p in products)
        {
            allowed.UnionWith([p.WholeCount, p.OpenedCount, p.WholeCount + p.OpenedCount, p.Batches.Count]);
            AddWeight(allowed, p.RemainingKg);
            AddDate(allowed, p.OldestDate);
            foreach (var b in p.Batches)
            {
                allowed.UnionWith([b.Count, b.SalePrice, Math.Round(b.SalePrice)]);
                AddWeight(allowed, b.RemainingKg);
                AddDate(allowed, b.ProductionDate);
            }
            foreach (var o in p.Opened)
            {
                AddWeight(allowed, o.RemainingKg);
                AddDate(allowed, o.ProductionDate);
            }
        }
        allowed.Add(products.Count);

        return SpokenNumbers(speech).Where(n => !allowed.Contains(n.Value)).Select(n => n.Text).ToList();
    }

    private static readonly Dictionary<string, int> NumberWords = new()
    {
        ["deux"] = 2, ["trois"] = 3, ["quatre"] = 4, ["cinq"] = 5, ["six"] = 6, ["sept"] = 7, ["huit"] = 8,
        ["neuf"] = 9, ["dix"] = 10, ["onze"] = 11, ["douze"] = 12, ["treize"] = 13, ["quatorze"] = 14,
        ["quinze"] = 15, ["seize"] = 16, ["vingt"] = 20, ["trente"] = 30, ["quarante"] = 40, ["cinquante"] = 50,
    };

    private static IEnumerable<(string Text, decimal Value)> SpokenNumbers(string speech)
    {
        // Les numéros d'étiquette (« SC-250831-1 ») identifient une unité : ce ne sont pas des quantités.
        var text = Regex.Replace(speech, @"\b[A-Z]{1,4}-\d{6}-\d+\b", " ");
        foreach (Match m in Regex.Matches(text, @"\d+(?:[.,]\d+)?"))
            yield return (m.Value, decimal.Parse(m.Value.Replace(',', '.'), CultureInfo.InvariantCulture));
        foreach (Match m in Regex.Matches(text.ToLowerInvariant(), @"\p{L}+"))
            if (NumberWords.TryGetValue(m.Value, out var value))
                yield return (m.Value, value);
    }

    private static void AddWeight(HashSet<decimal> allowed, decimal? kilograms)
    {
        if (kilograms is not { } kg)
            return;
        // « 3 kilos et 660 » : les grammes au-delà du kilo, exacts ou arrondis à la dizaine.
        var grams = (kg - Math.Floor(kg)) * 1000m;
        allowed.UnionWith([Math.Round(kg), Math.Round(kg, 1), Math.Round(kg, 2), Math.Round(kg, 3),
            Math.Round(kg * 1000m), Math.Round(kg * 100m) * 10m, Math.Floor(kg), Math.Floor(kg * 10m) / 10m,
            Math.Round(grams), Math.Round(grams / 10m) * 10m]);
    }

    private static void AddDate(HashSet<decimal> allowed, DateOnly? date)
    {
        if (date is { } d)
            allowed.UnionWith([d.Day, d.Month, d.Year]);
    }

    private static string Count(int count, string name) =>
        $"{count} {(count > 1 && !name.EndsWith('s') && !name.EndsWith('x') ? name.ToLowerInvariant() + "s" : name.ToLowerInvariant())}";

    private static string Kilos(decimal kilograms) =>
        kilograms < 1m ? $"{Math.Round(kilograms * 1000m / 10m) * 10m:0} grammes" : $"{Math.Round(kilograms, 1).ToString("0.#", French)} kilos";
}
