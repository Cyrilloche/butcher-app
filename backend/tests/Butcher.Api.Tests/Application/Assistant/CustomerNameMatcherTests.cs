using Butcher.Api.Application.Assistant;
using Xunit.Abstractions;

namespace Butcher.Api.Tests.Application.Assistant;

/// <summary>
/// Reconnaissance des clients dans une phrase transcrite (spike assistant vocal, étape 2).
/// Critère éliminatoire : aucun mauvais client choisi, sur tout le corpus (spike §5).
/// </summary>
public class CustomerNameMatcherTests(ITestOutputHelper output)
{
    private const int Martin = 1, Martine = 2, Josette = 3, Paul = 4, Moreau = 5, Gerard = 6;

    /// <summary>Les clients du jeu de phrases, choisis pour piéger la reconnaissance.</summary>
    private static readonly CustomerRef[] Customers =
    [
        new(Martin, "Martin", null),
        new(Martine, "Roux", "Martine"),
        new(Josette, "Dubois", "Josette"),
        new(Paul, "Lefèvre", "Paul"),
        new(Moreau, "Moreau", null),
        new(Gerard, "Gérard", null),
    ];

    /// <summary>Le ou les clients réellement cités dans chaque phrase ; F1 cite un nom inconnu.</summary>
    private static readonly Dictionary<string, int[]> Truth = new()
    {
        ["B1"] = [Martin], ["B2"] = [Martine], ["B3"] = [Josette], ["B4"] = [Gerard], ["B5"] = [Moreau],
        ["B6"] = [Paul], ["B7"] = [Martin], ["B8"] = [Martin], ["C1"] = [Martin], ["C2"] = [Gerard],
        ["C3"] = [Josette], ["C4"] = [Paul], ["C5"] = [Moreau], ["D1"] = [Martin], ["D2"] = [Gerard],
        ["D3"] = [Moreau], ["D4"] = [Josette], ["D5"] = [Paul], ["E1"] = [Martin], ["E2"] = [Gerard],
        ["E3"] = [Martin], ["E4"] = [Moreau], ["F3"] = [Martin], ["F4"] = [Martin], ["F5"] = [Gerard],
        ["F6"] = [Martin], ["F9"] = [Martin, Gerard], ["F10"] = [Gerard],
    };

    private readonly CustomerNameMatcher _matcher = new(Customers);

    [Theory]
    [InlineData("Moreau", "moraux")]
    [InlineData("Moreau", "moro")]
    [InlineData("Lefèvre", "Lefebvre")]
    [InlineData("Josette", "josette")]
    public void Encode_SameSound_SameCode(string a, string b) =>
        Assert.Equal(FrenchPhonetic.Encode(a), FrenchPhonetic.Encode(b));

    [Fact]
    public void Encode_MartinAndMartine_Differ() =>
        Assert.NotEqual(FrenchPhonetic.Encode("Martin"), FrenchPhonetic.Encode("Martine"));

    [Fact]
    public void Pseudonymize_KnownCustomer_ReplacesNameWithToken()
    {
        var result = _matcher.Pseudonymize("Vends deux saucissons à Madame Martin.");

        Assert.Equal("Vends deux saucissons à [CLIENT_1].", result.Text);
        var mention = Assert.Single(result.Mentions);
        Assert.Equal((MentionStatus.Matched, Martin), (mention.Status, mention.CustomerId!.Value));
    }

    [Fact]
    public void Pseudonymize_SameSoundDifferentSpelling_FindsCustomer()
    {
        var result = _matcher.Pseudonymize("Une livre de jambon pour les moraux.");

        Assert.Equal("Une livre de jambon pour [CLIENT_1].", result.Text);
        Assert.Equal(Moreau, Assert.Single(result.Mentions).CustomerId);
    }

    [Fact]
    public void Pseudonymize_UnknownName_IsRemoved()
    {
        var result = _matcher.Pseudonymize("Vends deux saucissons à Madame Petitjean.");

        Assert.Equal($"Vends deux saucissons à {CustomerNameMatcher.UnknownToken}.", result.Text);
        Assert.Equal(MentionStatus.Unknown, Assert.Single(result.Mentions).Status);
    }

    [Fact]
    public void Pseudonymize_ConfusableNameWithoutConfirmation_ChoosesNobody()
    {
        // « Martin » sans civilité : peut-être « Martine » mal transcrit.
        var result = _matcher.Pseudonymize("Mets un saucisson pour Martin.");

        Assert.Equal(MentionStatus.Ambiguous, Assert.Single(result.Mentions).Status);
        Assert.DoesNotContain("Martin", result.Text);
    }

    [Fact]
    public void Pseudonymize_TwoCustomersWithSameName_ChoosesNobody()
    {
        var matcher = new CustomerNameMatcher([new(1, "Moreau", "Jean"), new(2, "Moreau", "Anne")]);

        var result = matcher.Pseudonymize("Trois terrines aux Moreau.");

        Assert.Equal(MentionStatus.Ambiguous, Assert.Single(result.Mentions).Status);
    }

    [Fact]
    public void Pseudonymize_ProductAfterPreposition_IsNotAName()
    {
        var result = _matcher.Pseudonymize("Un petit saucisson à 8 euros pour la Josette.");

        Assert.Equal("Un petit saucisson à 8 euros pour [CLIENT_1].", result.Text);
    }

    [Fact]
    public void Corpus_NeverChoosesTheWrongCustomer_AndReportsScores()
    {
        var wrong = new List<string>();
        foreach (var engine in CorpusTranscripts.All.GroupBy(t => t.Engine))
        {
            int expected = 0, found = 0, missed = 0, removedUnknown = 0;
            foreach (var (_, phraseId, text) in engine)
            {
                var truth = Truth.GetValueOrDefault(phraseId, []);
                var result = _matcher.Pseudonymize(text);
                var chosen = result.Mentions.Where(m => m.Status == MentionStatus.Matched)
                    .Select(m => m.CustomerId!.Value).ToList();

                wrong.AddRange(chosen.Where(id => !truth.Contains(id))
                    .Select(id => $"{engine.Key} {phraseId} « {text} » → client {id}"));
                expected += truth.Length;
                found += truth.Count(chosen.Contains);
                missed += truth.Count(id => !chosen.Contains(id));
                if (phraseId == "F1" && result.Mentions.Any(m => m.Status == MentionStatus.Unknown))
                    removedUnknown++;
                if (truth.Any(id => !chosen.Contains(id)))
                    output.WriteLine($"  {engine.Key} {phraseId} manqué : « {text} » → « {result.Text} »");
            }
            output.WriteLine($"{engine.Key} : {found}/{expected} clients trouvés, {missed} à choisir à l'écran, "
                + $"nom inconnu retiré : {(removedUnknown == 1 ? "oui" : "non")}");
        }

        Assert.True(wrong.Count == 0, "Mauvais client choisi :\n" + string.Join("\n", wrong));
    }
}
