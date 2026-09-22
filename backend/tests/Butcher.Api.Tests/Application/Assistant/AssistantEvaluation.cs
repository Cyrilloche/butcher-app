using System.Text;
using System.Text.Json.Nodes;
using Butcher.Api.Application.Assistant;
using Butcher.Api.Domain.Enums;
using Butcher.Api.Infrastructure.Mistral;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace Butcher.Api.Tests.Application.Assistant;

/// <summary>
/// Banc de l'étape 3 du spike (docs/spike-assistant-vocal.md) : le LLM comprend-il l'intention et
/// remplit-il les bons champs, et les chiffres qu'il dit sont-ils ceux du serveur ?
/// </summary>
/// <remarks>
/// Appelle vraiment Mistral : ne tourne que si <c>ASSISTANT_EVAL=1</c>, sinon le test passe sans rien
/// faire. Modèles : <c>ASSISTANT_EVAL_MODELS</c> (séparés par des virgules). Clé : <c>MISTRAL_API_KEY</c>,
/// ou <c>development/.env</c>. Résultats phrase par phrase dans <c>development/assistant-corpus/results/</c>.
/// </remarks>
public class AssistantEvaluation(ITestOutputHelper output)
{
    private const int Martin = 1, Martine = 2, Josette = 3, Paul = 4, Moreau = 5, Gerard = 6;

    private static readonly CustomerRef[] Customers =
    [
        new(Martin, "Martin", null), new(Martine, "Roux", "Martine"), new(Josette, "Dubois", "Josette"),
        new(Paul, "Lefèvre", "Paul"), new(Moreau, "Moreau", null), new(Gerard, "Gérard", null),
    ];

    private static readonly CatalogProduct[] Catalog =
    [
        new("JB", "Jambon", SaleMode.ByWeight, true),
        new("SC", "Saucisson", SaleMode.ByWeight, false),
        new("TR", "Terrine", SaleMode.ByPiece, false),
    ];

    private static readonly SellableUnit[] Stock = BuildStock();

    private static SellableUnit[] BuildStock()
    {
        var sep2 = new DateOnly(2026, 9, 2);
        var sep16 = new DateOnly(2026, 9, 16);
        var units = new List<SellableUnit>();
        decimal[] weights = [0.284m, 0.301m, 0.312m, 0.296m, 0.341m, 0.275m, 0.322m, 0.358m, 0.289m, 0.305m, 0.318m, 0.262m];
        for (var i = 0; i < weights.Length; i++)
        {
            var date = i < 5 ? sep2 : sep16;
            units.Add(new(i + 1, $"SC-{date:yyMMdd}-{i + 1}", "SC", "Saucisson", SaleMode.ByWeight, false, i < 5 ? 1 : 2,
                date, i < 5 ? 24m : 26m, weights[i], weights[i], StockUnitStatus.Available));
        }
        units.Add(new(20, "JB-260902-1", "JB", "Jambon", SaleMode.ByWeight, true, 3, sep2, 28m, 7.850m, 4.120m, StockUnitStatus.Opened));
        units.Add(new(21, "JB-260916-1", "JB", "Jambon", SaleMode.ByWeight, true, 4, sep16, 28m, 7.420m, 7.420m, StockUnitStatus.Available));
        units.Add(new(22, "JB-260916-2", "JB", "Jambon", SaleMode.ByWeight, true, 4, sep16, 28m, 8.010m, 8.010m, StockUnitStatus.Available));
        for (var i = 0; i < 6; i++)
            units.Add(new(30 + i, $"TR-260916-{i + 1}", "TR", "Terrine", SaleMode.ByPiece, false, 5, sep16, 7.5m, null, null, StockUnitStatus.Available));
        return [.. units];
    }

    private enum Intent { Stock, Sale, None, StockAndSale }

    /// <param name="Weight">Grammes attendus (tolérance d'un gramme) ; null : aucun poids ne doit être donné.</param>
    private sealed record Line(string Code, int Quantity = 1, decimal? Weight = null, decimal? Price = null, bool Slice = false);

    /// <param name="Alternative">Autre lecture admise, pour une phrase ambiguë.</param>
    /// <param name="EmptySaleAccepted">Une vente sans ligne vaut aussi « pas compris » (demande incomplète).</param>
    private sealed record Expected(Intent Intent, string? StockCode = null, Line[]? Lines = null, bool Paid = false,
        Line[]? Alternative = null, bool EmptySaleAccepted = false);

    private static Expected StockOf(string? code) => new(Intent.Stock, code);
    private static Expected Sale(params Line[] lines) => new(Intent.Sale, Lines: lines);
    private static Line L(string code, int quantity = 1, decimal? Weight = null, decimal? Price = null, bool Slice = false) =>
        new(code, quantity, Weight, Price, Slice);

    /// <summary>Ce que chaque phrase du jeu de test doit donner (docs/assistant-vocal-phrases-test.md, colonne « Attendu »).</summary>
    private static readonly Dictionary<string, Expected> Expectations = new()
    {
        ["A1"] = StockOf("SC"), ["A2"] = StockOf("JB"), ["A3"] = StockOf("TR"), ["A4"] = StockOf(null),
        ["A5"] = StockOf("SC"), ["A6"] = StockOf("JB"), ["A7"] = StockOf("SC"), ["A8"] = StockOf("TR"),
        ["B1"] = Sale(L("SC", 2)), ["B2"] = Sale(L("SC")), ["B3"] = Sale(L("TR", 3)), ["B4"] = Sale(L("TR")),
        ["B5"] = Sale(L("SC"), L("TR")), ["B6"] = Sale(L("SC", 2)), ["B7"] = Sale(L("SC", 2)), ["B8"] = Sale(L("SC", 2)),
        ["C1"] = Sale(L("SC", Weight: 300)), ["C2"] = Sale(L("SC")), ["C3"] = Sale(L("SC", Price: 8)),
        ["C4"] = Sale(L("SC", Weight: 350)), ["C5"] = Sale(L("SC", Weight: 500)),
        ["D1"] = Sale(L("JB", Weight: 200, Slice: true)), ["D2"] = Sale(L("JB", Weight: 250, Slice: true)),
        ["D3"] = Sale(L("JB", Weight: 500, Slice: true)), ["D4"] = Sale(L("JB", Slice: true)), ["D5"] = Sale(L("JB")),
        ["E1"] = Sale(L("SC", 2)) with { Paid = true }, ["E2"] = Sale(L("TR")),
        ["E3"] = Sale(L("SC", 2)) with { Paid = true }, ["E4"] = Sale(L("TR", 3)),
        ["F1"] = Sale(L("SC", 2)), ["F2"] = Sale(L("SC", 2)),
        ["F3"] = new(Intent.None, EmptySaleAccepted: true), ["F4"] = new(Intent.None, EmptySaleAccepted: true),
        ["F5"] = Sale(L("SC", 10)), ["F6"] = new(Intent.None, EmptySaleAccepted: true),
        ["F7"] = new(Intent.None), ["F8"] = new(Intent.None),
        ["F9"] = Sale(L("SC", 2), L("TR")) with { Alternative = [L("SC", 2)] },
        ["F10"] = new(Intent.StockAndSale, "SC", [L("SC", 2)]),
    };

    private static readonly Dictionary<string, int> CustomerTruth = CorpusTranscripts.All
        .Where(r => r.Source == "phrase" && r.Truth.Length > 0).ToDictionary(r => r.PhraseId, r => r.Truth[0]);

    [Fact]
    public async Task Evaluate()
    {
        if (Environment.GetEnvironmentVariable("ASSISTANT_EVAL") != "1")
        {
            output.WriteLine("Banc non lancé : ASSISTANT_EVAL=1 pour appeler Mistral.");
            return;
        }

        var models = (Environment.GetEnvironmentVariable("ASSISTANT_EVAL_MODELS") ?? "ministral-14b-2512")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var sources = (Environment.GetEnvironmentVariable("ASSISTANT_EVAL_SOURCES") ?? "phrase,voxtral").Split(',');
        var delay = TimeSpan.FromMilliseconds(int.Parse(Environment.GetEnvironmentVariable("ASSISTANT_EVAL_DELAY_MS") ?? "4500"));
        var rows = CorpusTranscripts.All.Where(r => sources.Contains(r.Source) && Expectations.ContainsKey(r.PhraseId)).ToList();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["MISTRAL_API_KEY"] = ApiKey() }).Build();
        var client = new MistralClient(new HttpClient { BaseAddress = new Uri(MistralClient.BaseAddress), Timeout = TimeSpan.FromSeconds(60) },
            configuration, NullLogger<MistralClient>.Instance);

        foreach (var model in models)
        {
            var engine = new AssistantEngine(client, model);
            var csv = new StringBuilder("source;phrase;texte;attendu;intention;champs;client;chiffres_inventes;outils;phrase_llm;phrase_dite;ms;jetons\n");
            var scores = new Dictionary<string, Score>();
            foreach (var (source, phraseId, text, _, _) in rows)
            {
                // Débit du compte (Ministral 14B : 30 requêtes par minute, deux par question de stock).
                await Task.Delay(delay);
                var (reply, trace) = await engine.AskAsync(text, Customers, Catalog, Stock);
                var expected = Expectations[phraseId];
                var intentOk = IntentMatches(expected, reply, trace);
                var fieldsOk = expected.Intent is Intent.Sale or Intent.StockAndSale ? FieldsMatch(expected, trace) : (bool?)null;
                var stockOk = expected.Intent is Intent.Stock or Intent.StockAndSale ? StockCodeMatches(expected, trace) : (bool?)null;
                var customer = CustomerOutcome(phraseId, reply);

                var s = scores.TryGetValue(source, out var existing) ? existing : scores[source] = new Score();
                s.Add(intentOk, fieldsOk, stockOk, customer, trace);
                csv.Append(string.Join(';', source, phraseId, Csv(text), expected.Intent, intentOk, fieldsOk?.ToString() ?? "",
                    customer, string.Join(' ', trace.InventedNumbers), Csv(string.Join(" | ", trace.ToolCalls.Select(c => $"{c.Name} {c.Arguments}"))),
                    Csv(trace.LlmSpeech ?? ""), Csv(reply.Speech), (int)trace.Elapsed.TotalMilliseconds,
                    trace.PromptTokens + trace.CompletionTokens)).Append('\n');

                if (!intentOk || fieldsOk == false || stockOk == false || customer == "faux" || trace.InventedNumbers.Count > 0)
                    output.WriteLine($"  {model} {source} {phraseId} « {text} » → {string.Join(" | ", trace.ToolCalls.Select(c => $"{c.Name} {c.Arguments}"))}"
                        + (trace.InventedNumbers.Count > 0 ? $" — chiffres inventés {string.Join(',', trace.InventedNumbers)} dans « {trace.LlmSpeech} »" : ""));
            }

            foreach (var (source, s) in scores)
                output.WriteLine(s.Summary(model, source));
            var resultsDir = Path.Combine(RepositoryRoot(), "development", "assistant-corpus", "results");
            Directory.CreateDirectory(resultsDir);
            await File.WriteAllTextAsync(Path.Combine(resultsDir, $"assistant-eval-{model}.csv"), csv.ToString());
        }
    }

    private sealed class Score
    {
        private int _phrases, _intent, _fieldsTotal, _fields, _stockTotal, _stock, _customerTotal, _customer, _wrongCustomer, _invented, _spoken, _tokens;
        private readonly List<double> _ms = [];

        public void Add(bool intentOk, bool? fieldsOk, bool? stockOk, string customer, AssistantTrace trace)
        {
            _phrases++;
            _intent += intentOk ? 1 : 0;
            if (fieldsOk is { } f) { _fieldsTotal++; _fields += f ? 1 : 0; }
            if (stockOk is { } k) { _stockTotal++; _stock += k ? 1 : 0; }
            if (customer != "") { _customerTotal++; _customer += customer == "juste" ? 1 : 0; _wrongCustomer += customer == "faux" ? 1 : 0; }
            if (trace.LlmSpeech is not null) { _spoken++; _invented += trace.InventedNumbers.Count > 0 ? 1 : 0; }
            _tokens += trace.PromptTokens + trace.CompletionTokens;
            _ms.Add(trace.Elapsed.TotalMilliseconds);
        }

        public string Summary(string model, string source)
        {
            _ms.Sort();
            return $"| {model} | {source} | {Pct(_intent, _phrases)} | {Pct(_fields, _fieldsTotal)} | {Pct(_stock, _stockTotal)} "
                + $"| {Pct(_customer, _customerTotal)} | {_wrongCustomer} | {_invented}/{_spoken} "
                + $"| {_ms[_ms.Count / 2] / 1000:0.00} s | {_ms[^1] / 1000:0.00} s | {_tokens / _phrases} |";
        }

        private static string Pct(int ok, int total) => total == 0 ? "—" : $"{ok}/{total} ({100 * ok / total} %)";
    }

    private static bool IntentMatches(Expected expected, AssistantReply reply, AssistantTrace trace)
    {
        var hasStock = trace.ToolCalls.Any(c => c.Name == "get_stock");
        var hasSale = trace.ToolCalls.Any(c => c.Name == "draft_sale");
        var emptySale = hasSale && SaleLines(trace).Count == 0;
        return expected.Intent switch
        {
            Intent.Stock => hasStock && !hasSale,
            Intent.Sale => hasSale && !hasStock,
            Intent.StockAndSale => hasStock && hasSale,
            Intent.None => reply.Kind == AssistantReplyKind.NotUnderstood
                || (expected.EmptySaleAccepted && !hasStock && emptySale),
            _ => false,
        };
    }

    private static bool StockCodeMatches(Expected expected, AssistantTrace trace)
    {
        var call = trace.ToolCalls.FirstOrDefault(c => c.Name == "get_stock");
        if (call is null)
            return false;
        var code = (JsonNode.Parse(call.Arguments) as JsonObject)?["product_code"]?.GetValue<string>();
        return string.Equals(string.IsNullOrEmpty(code) ? null : code, expected.StockCode, StringComparison.OrdinalIgnoreCase);
    }

    private static bool FieldsMatch(Expected expected, AssistantTrace trace)
    {
        var call = trace.ToolCalls.FirstOrDefault(c => c.Name == "draft_sale");
        if (call is null)
            return false;
        var args = JsonNode.Parse(call.Arguments) as JsonObject ?? [];
        var paid = args["paid"] is JsonValue p && p.TryGetValue<bool>(out var b) && b;
        var lines = SaleLines(trace);
        return paid == expected.Paid
            && (LinesMatch(expected.Lines!, lines) || (expected.Alternative is { } alt && LinesMatch(alt, lines)));
    }

    private static bool LinesMatch(Line[] expected, List<Line> actual) =>
        expected.Length == actual.Count
        && expected.OrderBy(l => l.Code).Zip(actual.OrderBy(l => l.Code)).All(pair =>
            pair.First.Code == pair.Second.Code
            && pair.First.Slice == pair.Second.Slice
            && (pair.First.Slice || pair.First.Quantity == pair.Second.Quantity)
            && (pair.First.Weight is null ? pair.Second.Weight is null : pair.Second.Weight is { } w && Math.Abs(w - pair.First.Weight.Value) <= 1)
            && (pair.First.Price is null ? pair.Second.Price is null : pair.Second.Price == pair.First.Price));

    private static List<Line> SaleLines(AssistantTrace trace)
    {
        var call = trace.ToolCalls.FirstOrDefault(c => c.Name == "draft_sale");
        if (call is null || JsonNode.Parse(call.Arguments) is not JsonObject args || args["lines"] is not JsonArray lines)
            return [];
        return lines.OfType<JsonObject>().Select(l => new Line(
            (l["product_code"] as JsonValue)?.ToString().ToUpperInvariant() ?? "",
            l["quantity"] is JsonValue q && q.TryGetValue<decimal>(out var qd) ? (int)qd : 1,
            l["weight_g"] is JsonValue w && w.TryGetValue<decimal>(out var wd) ? wd : null,
            l["price_eur"] is JsonValue pr && pr.TryGetValue<decimal>(out var pd) ? pd : null,
            l["slice"] is JsonValue s && s.TryGetValue<bool>(out var sb) && sb)).ToList();
    }

    /// <summary>« juste », « faux » (un autre client : éliminatoire), « à choisir », ou vide si la phrase ne cite personne.</summary>
    private static string CustomerOutcome(string phraseId, AssistantReply reply)
    {
        if (reply.Draft is null)
            return "";
        var chosen = reply.Draft.CustomerId;
        if (!CustomerTruth.TryGetValue(phraseId, out var truth))
            return chosen is null ? "" : "faux";
        return chosen is null ? "à choisir" : chosen == truth ? "juste" : "faux";
    }

    private static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";

    private static string ApiKey()
    {
        var key = Environment.GetEnvironmentVariable("MISTRAL_API_KEY");
        if (!string.IsNullOrWhiteSpace(key))
            return key;
        var env = Path.Combine(RepositoryRoot(), "development", ".env");
        return File.ReadLines(env).FirstOrDefault(l => l.StartsWith("MISTRAL_API_KEY=", StringComparison.Ordinal))?["MISTRAL_API_KEY=".Length..].Trim()
            ?? throw new InvalidOperationException("MISTRAL_API_KEY absente.");
    }

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "docs")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Racine du dépôt introuvable.");
    }
}
