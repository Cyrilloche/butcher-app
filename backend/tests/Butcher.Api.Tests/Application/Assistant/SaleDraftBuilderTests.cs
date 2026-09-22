using Butcher.Api.Application.Assistant;
using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Tests.Application.Assistant;

/// <summary>Choix des unités d'une vente dictée (cadrage §6).</summary>
public class SaleDraftBuilderTests
{
    private static readonly DateOnly Old = new(2026, 9, 1), Recent = new(2026, 9, 15);

    private static SellableUnit Saucisson(int id, DateOnly date, decimal weight, decimal pricePerKg = 20m) =>
        new(id, $"SC-{id}", "SC", "Saucisson", SaleMode.ByWeight, false, date == Old ? 1 : 2, date, pricePerKg,
            weight, weight, StockUnitStatus.Available);

    private static SellableUnit Jambon(int id, DateOnly date, decimal weight, decimal remaining, StockUnitStatus status) =>
        new(id, $"JB-{id}", "JB", "Jambon", SaleMode.ByWeight, true, date == Old ? 3 : 4, date, 25m,
            weight, remaining, status);

    private static SellableUnit Terrine(int id) =>
        new(id, $"TR-{id}", "TR", "Terrine", SaleMode.ByPiece, false, 5, Old, 6m, null, null, StockUnitStatus.Available);

    private static readonly SellableUnit[] Stock =
    [
        Saucisson(1, Recent, 0.250m), Saucisson(2, Old, 0.310m), Saucisson(3, Old, 0.420m), Saucisson(4, Recent, 0.360m),
        Jambon(10, Recent, 7m, 7m, StockUnitStatus.Available), Jambon(11, Old, 8m, 3.5m, StockUnitStatus.Opened),
        Jambon(12, Recent, 7.5m, 6m, StockUnitStatus.Opened),
        Terrine(20), Terrine(21),
    ];

    private static SaleDraft Build(params DraftLineRequest[] requests) =>
        SaleDraftBuilder.Build(customerId: 7, paid: false, requests, Stock);

    [Fact]
    public void NothingSaid_OldestUnitsFirst()
    {
        var draft = Build(new DraftLineRequest("SC", 2, null, null, false));

        Assert.Equal([2, 3], draft.Lines.Select(l => l.StockUnitId));
        Assert.All(draft.Lines, l => Assert.True(l.IsFullSale));
        Assert.Empty(draft.Warnings);
    }

    [Fact]
    public void WeightSaid_ClosestUnit()
    {
        var draft = Build(new DraftLineRequest("SC", 1, 350m, null, false));

        Assert.Equal(4, Assert.Single(draft.Lines).StockUnitId);
    }

    [Fact]
    public void PriceSaid_ClosestTheoreticalPrice()
    {
        // 0,250 kg × 20 € = 5 € ; 0,420 × 20 = 8,40 € : « un saucisson à 8 euros » → l'unité 3.
        var draft = Build(new DraftLineRequest("SC", 1, null, 8m, false));

        Assert.Equal(3, Assert.Single(draft.Lines).StockUnitId);
    }

    [Fact]
    public void Slice_OldestOpenedHam_WithSoldWeight()
    {
        var draft = Build(new DraftLineRequest("JB", null, 200m, null, true));

        var line = Assert.Single(draft.Lines);
        Assert.Equal((11, false, 0.2m), (line.StockUnitId, line.IsFullSale, line.SoldWeight!.Value));
    }

    [Fact]
    public void SliceWithoutWeight_KeepsTheHam_AndAsksForTheWeight()
    {
        var draft = Build(new DraftLineRequest("JB", null, null, null, true));

        Assert.Null(Assert.Single(draft.Lines).SoldWeight);
        Assert.Contains("à saisir", Assert.Single(draft.Warnings));
    }

    [Fact]
    public void SliceHeavierThanWhatIsLeft_WeightLeftToTheUser()
    {
        var draft = Build(new DraftLineRequest("JB", null, 4000m, null, true));

        Assert.Null(Assert.Single(draft.Lines).SoldWeight);
        Assert.Single(draft.Warnings);
    }

    [Fact]
    public void WholeHam_NeverAnOpenedOne()
    {
        var draft = Build(new DraftLineRequest("JB", 1, null, null, false));

        Assert.Equal(10, Assert.Single(draft.Lines).StockUnitId);
    }

    [Fact]
    public void ByPiece_ProposesPieces()
    {
        var draft = Build(new DraftLineRequest("TR", 2, null, null, false));

        Assert.Equal([20, 21], draft.Lines.Select(l => l.StockUnitId));
    }

    [Fact]
    public void MoreThanInStock_ProposesWhatThereIs_AndSaysSo()
    {
        var draft = Build(new DraftLineRequest("SC", 10, null, null, false));

        Assert.Equal(4, draft.Lines.Count);
        Assert.Contains("4 en stock", Assert.Single(draft.Warnings));
    }

    [Fact]
    public void UnknownProduct_Warns_AndKeepsTheRest()
    {
        var draft = Build(new DraftLineRequest("CH", 2, null, null, false), new DraftLineRequest("TR", 1, null, null, false));

        Assert.Equal(20, Assert.Single(draft.Lines).StockUnitId);
        Assert.Contains("CH", Assert.Single(draft.Warnings));
    }

    [Fact]
    public void SameProductTwice_NeverTheSameUnitTwice()
    {
        var draft = Build(new DraftLineRequest("SC", 1, null, null, false), new DraftLineRequest("SC", 1, null, null, false));

        Assert.Equal([2, 3], draft.Lines.Select(l => l.StockUnitId));
    }

    [Fact]
    public void KeepsCustomerAndPayment_NoAmount()
    {
        var draft = Build(new DraftLineRequest("TR", 1, null, null, false));

        Assert.Equal((7, false), (draft.CustomerId!.Value, draft.Paid));
    }
}
