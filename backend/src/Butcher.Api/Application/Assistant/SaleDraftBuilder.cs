using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Application.Assistant;

/// <summary>Ce que le LLM a compris d'une ligne de vente dictée (outil <c>draft_sale</c>).</summary>
/// <param name="WeightGrams">Poids visé par unité ; pour une tranche, le poids à couper.</param>
/// <param name="PriceEuros">Prix visé par unité.</param>
/// <param name="Slice">Vente à la tranche (« 200 g de jambon », « quatre tranches »).</param>
public sealed record DraftLineRequest(string ProductCode, int? Quantity, decimal? WeightGrams, decimal? PriceEuros, bool Slice);

/// <param name="SoldWeight">Kilogrammes vendus pour une tranche ; null pour une unité entière ou un poids à saisir.</param>
public sealed record DraftLine(int StockUnitId, bool IsFullSale, decimal? SoldWeight);

/// <summary>
/// Brouillon d'une vente, à ouvrir dans le formulaire existant (FR-016) : il ne porte
/// aucun montant, que le formulaire calcule comme pour une saisie à la main.
/// </summary>
public sealed record SaleDraft(int? CustomerId, bool Paid, IReadOnlyList<DraftLine> Lines, IReadOnlyList<string> Warnings);

/// <summary>
/// Choisit les unités d'une vente dictée (RF-35, FR-014) : les plus anciennes d'abord ; si un poids est
/// dit, la plus proche de ce poids ; si un prix est dit, la plus proche de ce prix ; pour une tranche,
/// l'unité entamée la plus ancienne. N'écrit rien : c'est l'utilisateur qui enregistre.
/// </summary>
public static class SaleDraftBuilder
{
    public static SaleDraft Build(int? customerId, bool paid, IEnumerable<DraftLineRequest> requests,
        IReadOnlyList<SellableUnit> stock)
    {
        var lines = new List<DraftLine>();
        var warnings = new List<string>();
        var taken = new HashSet<int>();

        foreach (var request in requests)
        {
            var units = stock.Where(u => string.Equals(u.ProductCode, request.ProductCode, StringComparison.OrdinalIgnoreCase)).ToList();
            if (units.Count == 0)
            {
                warnings.Add($"Produit « {request.ProductCode} » introuvable dans le stock.");
                continue;
            }

            var product = units[0];
            if (request.Slice && product.AllowPartialSale)
                AddSlice(request, units, taken, lines, warnings);
            else
            {
                if (request.Slice)
                    warnings.Add($"{product.ProductName} ne se vend pas à la tranche : unité entière proposée.");
                AddWholeUnits(request, units, taken, lines, warnings);
            }
        }

        return new SaleDraft(customerId, paid, lines, warnings);
    }

    private static void AddWholeUnits(DraftLineRequest request, List<SellableUnit> units, HashSet<int> taken,
        List<DraftLine> lines, List<string> warnings)
    {
        var quantity = Math.Max(request.Quantity ?? 1, 1);
        var available = units.Where(u => u.Status == StockUnitStatus.Available && !taken.Contains(u.Id))
            .OrderBy(u => u.ProductionDate).ThenBy(u => u.Id).ToList();

        IEnumerable<SellableUnit> ordered = available;
        if (request.WeightGrams is { } grams && available.All(u => u.Weight is not null))
            ordered = available.OrderBy(u => Math.Abs(u.Weight!.Value * 1000m - grams)).ThenBy(u => u.ProductionDate).ThenBy(u => u.Id);
        else if (request.PriceEuros is { } euros)
            ordered = available.OrderBy(u => Math.Abs(TheoreticalPrice(u) - euros)).ThenBy(u => u.ProductionDate).ThenBy(u => u.Id);

        var chosen = ordered.Take(quantity).ToList();
        foreach (var unit in chosen)
        {
            taken.Add(unit.Id);
            lines.Add(new DraftLine(unit.Id, IsFullSale: true, SoldWeight: null));
        }

        if (chosen.Count < quantity)
            warnings.Add(chosen.Count == 0
                ? $"Plus aucun {units[0].ProductName.ToLowerInvariant()} entier en stock."
                : $"{quantity} {units[0].ProductName.ToLowerInvariant()} demandés, {chosen.Count} en stock.");
    }

    private static void AddSlice(DraftLineRequest request, List<SellableUnit> units, HashSet<int> taken,
        List<DraftLine> lines, List<string> warnings)
    {
        var name = units[0].ProductName.ToLowerInvariant();
        // L'entamé le plus ancien d'abord ; à défaut, on entame l'unité intacte la plus ancienne.
        var unit = units.Where(u => !taken.Contains(u.Id) && u.RemainingWeight > 0)
            .OrderBy(u => u.Status == StockUnitStatus.Opened ? 0 : 1)
            .ThenBy(u => u.ProductionDate).ThenBy(u => u.Id)
            .FirstOrDefault();
        if (unit is null)
        {
            warnings.Add($"Plus de {name} à couper en stock.");
            return;
        }

        taken.Add(unit.Id);
        if (request.WeightGrams is not { } grams)
        {
            warnings.Add($"Poids de la tranche de {name} à saisir.");
            lines.Add(new DraftLine(unit.Id, IsFullSale: false, SoldWeight: null));
            return;
        }

        var kilograms = grams / 1000m;
        if (kilograms > unit.RemainingWeight)
        {
            warnings.Add($"Il reste moins de {grams:0} g sur ce {name} : poids à saisir.");
            lines.Add(new DraftLine(unit.Id, IsFullSale: false, SoldWeight: null));
            return;
        }

        lines.Add(new DraftLine(unit.Id, IsFullSale: false, SoldWeight: kilograms));
    }

    /// <summary>Prix d'une unité vendue en entière, pour la comparer au prix dit ; jamais renvoyé.</summary>
    private static decimal TheoreticalPrice(SellableUnit unit) =>
        unit.SaleMode == SaleMode.ByWeight && unit.Weight is { } weight ? weight * unit.SalePrice : unit.SalePrice;
}
