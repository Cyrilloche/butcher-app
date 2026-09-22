using Butcher.Api.Application.Assistant;
using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Tests.Application.Assistant;

/// <summary>Réponse à une question de stock et garde-fou sur les chiffres (cadrage §7).</summary>
public class StockSummaryBuilderTests
{
    private static readonly DateOnly Sep2 = new(2026, 9, 2), Sep15 = new(2026, 9, 15);

    private static readonly SellableUnit[] Stock =
    [
        new(1, "SC-260902-1", "SC", "Saucisson", SaleMode.ByWeight, false, 1, Sep2, 20m, 0.300m, 0.300m, StockUnitStatus.Available),
        new(2, "SC-260902-2", "SC", "Saucisson", SaleMode.ByWeight, false, 1, Sep2, 20m, 0.312m, 0.312m, StockUnitStatus.Available),
        new(3, "SC-260915-1", "SC", "Saucisson", SaleMode.ByWeight, false, 2, Sep15, 22m, 0.290m, 0.290m, StockUnitStatus.Available),
        new(10, "JB-260915-1", "JB", "Jambon", SaleMode.ByWeight, true, 3, Sep15, 25m, 7m, 7m, StockUnitStatus.Available),
        new(11, "JB-260902-1", "JB", "Jambon", SaleMode.ByWeight, true, 4, Sep2, 25m, 8m, 4.1m, StockUnitStatus.Opened),
        new(20, "TR-260902-1", "TR", "Terrine", SaleMode.ByPiece, false, 5, Sep2, 6m, null, null, StockUnitStatus.Available),
    ];

    [Fact]
    public void Build_CountsUnitsAndRemainingWeight_PerBatch()
    {
        var saucisson = Assert.Single(StockSummaryBuilder.Build(Stock, "SC"));

        Assert.Equal((3, 0, 0.902m, Sep2), (saucisson.WholeCount, saucisson.OpenedCount, saucisson.RemainingKg!.Value, saucisson.OldestDate!.Value));
        Assert.Equal([2, 1], saucisson.Batches.Select(b => b.Count));
    }

    [Fact]
    public void Build_OpenedHam_CountsWhatIsLeft_NotTheOriginalWeight()
    {
        var jambon = Assert.Single(StockSummaryBuilder.Build(Stock, "JB"));

        Assert.Equal((1, 1, 11.1m), (jambon.WholeCount, jambon.OpenedCount, jambon.RemainingKg!.Value));
    }

    [Fact]
    public void Speech_EssentialsOnly()
    {
        var speech = StockSummaryBuilder.Speech(StockSummaryBuilder.Build(Stock, "SC"));

        Assert.Equal("Il te reste 3 saucissons, environ 900 grammes. Les plus anciens datent du 2 septembre.", speech);
    }

    [Fact]
    public void Speech_Ham_SaysWholeAndOpenedApart()
    {
        var speech = StockSummaryBuilder.Speech(StockSummaryBuilder.Build(Stock, "JB"));

        Assert.StartsWith("Il te reste 1 jambon entier, et 1 entamé dont il reste environ 4,1 kilos", speech);
    }

    [Fact]
    public void Speech_Nothing_SaysSo()
    {
        Assert.Equal("Il ne te reste plus de chorizo.", StockSummaryBuilder.Speech([], "Chorizo"));
    }

    [Theory]
    [InlineData("Il te reste 3 saucissons, environ 0,9 kilo. Les plus anciens datent du 2 septembre.")]
    [InlineData("Il te reste trois saucissons, à peu près 900 grammes.")]
    [InlineData("Il te reste 3 saucissons : 2 du 2 septembre et 1 du 15 septembre.")]
    public void InventedNumbers_FaithfulSpeech_None(string speech) =>
        Assert.Empty(StockSummaryBuilder.InventedNumbers(speech, StockSummaryBuilder.Build(Stock, "SC")));

    [Theory]
    [InlineData("Il te reste 4 saucissons.", "4")]
    [InlineData("Il te reste 3 saucissons, environ 1,5 kilo.", "1,5")]
    [InlineData("Il te reste douze saucissons.", "douze")]
    public void InventedNumbers_NumberNotInTheData_Caught(string speech, string invented) =>
        Assert.Equal([invented], StockSummaryBuilder.InventedNumbers(speech, StockSummaryBuilder.Build(Stock, "SC")));
}
