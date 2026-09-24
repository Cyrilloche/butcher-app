using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Butcher.Api.Domain.Enums;

namespace Butcher.Api.Application.Assistant;

public sealed record CatalogProduct(string Code, string Name, SaleMode SaleMode, bool AllowPartialSale);

/// <summary>Les trois issues d'une demande comprise ou non (FR-006) ; une erreur, elle, est une exception.</summary>
public enum AssistantReplyKind
{
    StockAnswer,
    SaleDraft,
    NotUnderstood,
}

/// <summary>Réponse de l'assistant au téléphone : une phrase à dire, et de quoi afficher le détail ou le formulaire.</summary>
/// <param name="Heard">Ce que l'assistant a entendu, affiché avec chaque réponse (FR-007).</param>
/// <param name="Stock">Détail d'une réponse de stock, affiché sous la phrase dite (FR-010).</param>
/// <param name="Draft">Brouillon de vente, à ouvrir dans le formulaire « Nouvelle vente » (FR-016).</param>
public sealed record AssistantReply(AssistantReplyKind Kind, string Speech, string Heard,
    IReadOnlyList<ProductStock>? Stock, SaleDraft? Draft)
{
    /// <summary>Identifiant de la demande journalisée : sert à demander la voix de la réponse (FR-020).</summary>
    public long RequestId { get; init; }
}

/// <summary>Ce qui s'est passé, pour le banc d'évaluation ; n'est jamais renvoyé au téléphone.</summary>
/// <param name="Pseudonymized">Le texte tel que le LLM l'a reçu, sans nom de client (FR-019).</param>
public sealed record AssistantTrace(string Pseudonymized, IReadOnlyList<ToolCall> ToolCalls,
    int PromptTokens, int CompletionTokens, TimeSpan Elapsed);

/// <summary>
/// La chaîne de l'assistant (RF-34, RF-35 ; ADR-012) : pseudonymisation des clients, un appel au LLM avec
/// trois outils, exécution par le backend. Le LLM choisit l'intention et remplit des champs ; les unités,
/// le client, les chiffres et la phrase dite viennent du backend (FR-011). Rien n'est écrit en base.
/// </summary>
/// <remarks>
/// La phrase dite n'est jamais écrite par le LLM : au banc d'évaluation, il a annoncé « 1 jambon entier » pour 2,
/// avec un chiffre qui existait ailleurs dans les données, erreur qu'aucun contrôle des chiffres ne voyait
/// (specs/006-assistant-vocal, research R-06).
/// </remarks>
public sealed class AssistantEngine(IMistralClient mistral, string model)
{
    public const string NotUnderstoodSpeech =
        "Je n'ai pas compris. Tu peux me demander ce qu'il reste en stock, ou me dicter une vente.";
    public const string SaleDraftSpeech = "Voilà la vente, vérifie-la avant d'enregistrer.";
    public const string UnknownProductSpeech = "Je ne connais pas ce produit.";

    public async Task<(AssistantReply Reply, AssistantTrace Trace)> AskAsync(string text,
        IReadOnlyList<CustomerRef> customers, IReadOnlyList<CatalogProduct> catalog, IReadOnlyList<SellableUnit> stock,
        CancellationToken cancellationToken = default)
    {
        var watch = Stopwatch.StartNew();
        var pseudonymized = new CustomerNameMatcher(customers).Pseudonymize(text);
        var messages = new JsonArray
        {
            new JsonObject { ["role"] = "system", ["content"] = SystemPrompt(catalog) },
            new JsonObject { ["role"] = "user", ["content"] = pseudonymized.Text },
        };

        var result = await mistral.ChatAsync(model, messages, Tools(catalog), "any", cancellationToken);

        IReadOnlyList<ProductStock>? stockAnswer = null;
        string? speech = null;
        SaleDraft? draft = null;

        var stockCall = result.ToolCalls.FirstOrDefault(c => c.Name == "get_stock");
        if (stockCall is not null)
        {
            var code = ReadString(Parse(stockCall.Arguments), "product_code");
            var product = catalog.FirstOrDefault(p => string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase));
            if (code is not null && product is null)
            {
                // Un code hors catalogue n'est jamais remplacé par un autre produit (FR-012).
                speech = UnknownProductSpeech;
            }
            else
            {
                stockAnswer = StockSummaryBuilder.Build(stock, product?.Code);
                speech = StockSummaryBuilder.Speech(stockAnswer, product?.Name);
            }
        }

        var saleCall = result.ToolCalls.FirstOrDefault(c => c.Name == "draft_sale");
        if (saleCall is not null)
        {
            draft = BuildDraft(Parse(saleCall.Arguments), pseudonymized, stock);
            if (result.ToolCalls.Count(c => c.Name == "draft_sale") > 1)
                draft = draft with { Warnings = [.. draft.Warnings, "Une seule vente à la fois : seule la première a été préparée."] };
            speech = speech is null ? SaleDraftSpeech : $"{speech} {SaleDraftSpeech}";
        }

        var kind = draft is not null ? AssistantReplyKind.SaleDraft
            : stockAnswer is not null ? AssistantReplyKind.StockAnswer
            : AssistantReplyKind.NotUnderstood;
        var reply = new AssistantReply(kind, speech ?? NotUnderstoodSpeech, text, stockAnswer, draft);
        var trace = new AssistantTrace(pseudonymized.Text, result.ToolCalls,
            result.PromptTokens, result.CompletionTokens, watch.Elapsed);
        return (reply, trace);
    }

    private static SaleDraft BuildDraft(JsonObject args, PseudonymizedText pseudonymized, IReadOnlyList<SellableUnit> stock)
    {
        var token = ReadString(args, "customer_token")?.Trim('[', ']', ' ').ToUpperInvariant();
        var customerId = pseudonymized.Mentions
            .FirstOrDefault(m => m.Status == MentionStatus.Matched && m.Token.Trim('[', ']') == token)?.CustomerId;

        var requests = (args["lines"] as JsonArray ?? [])
            .OfType<JsonObject>()
            .Select(l => new DraftLineRequest(
                ReadString(l, "product_code") ?? "",
                (int?)ReadDecimal(l, "quantity"),
                ReadDecimal(l, "weight_g"),
                ReadDecimal(l, "price_eur"),
                ReadBool(l, "slice") ?? false))
            .ToList();

        var draft = SaleDraftBuilder.Build(customerId, ReadBool(args, "paid") ?? false, requests, stock);
        var warnings = draft.Warnings.ToList();
        if (customerId is null)
        {
            var heard = pseudonymized.Mentions.FirstOrDefault(m => m.Status != MentionStatus.Matched)?.Heard;
            warnings.Insert(0, heard is null ? "Client à choisir." : $"Client à choisir : « {heard} » n'a pas été reconnu.");
        }
        if (requests.Count == 0)
            warnings.Add("Produit à choisir.");
        return draft with { Warnings = warnings };
    }

    private static string SystemPrompt(IReadOnlyList<CatalogProduct> catalog)
    {
        var products = string.Join("\n", catalog.Select(p =>
            $"- {p.Code} : {p.Name} ({(p.SaleMode == SaleMode.ByWeight ? "au poids" : "à la pièce")}"
            + $"{(p.AllowPartialSale ? ", se vend aussi à la tranche" : "")})"));
        return $"""
            Tu es l'assistant vocal de Saloir, l'outil de gestion d'une petite charcuterie artisanale.
            Tu reçois une phrase dictée puis transcrite automatiquement : elle peut contenir des erreurs de transcription.

            Réponds toujours en appelant un outil :
            - get_stock : une question sur ce qui reste en stock ;
            - draft_sale : une vente (« vends », « mets », « X a pris… », ou simplement « deux saucissons à madame Y ») ;
            - not_understood : tout le reste (hors sujet, annulation ou modification d'une vente, phrase incomplète).
            Une phrase qui pose une question de stock et demande une vente appelle les deux outils ; une vente seule n'appelle pas get_stock.

            Catalogue, seuls codes admis :
            {products}
            Un produit absent du catalogue, ou un mot que tu ne reconnais pas comme un produit du catalogue, n'est jamais remplacé
            par un produit qui lui ressemble : appelle not_understood.

            Clients : leurs noms sont remplacés par des jetons comme [CLIENT_1]. Recopie le jeton dans customer_token.
            [CLIENT_INCONNU] ou aucun jeton : laisse customer_token vide. Une vente ne concerne qu'un client : s'il y en a plusieurs, garde le premier.

            Quantité : nombre d'unités. Poids en grammes : « une livre » = 500, « un demi-kilo » = 500, « un saucisson de 350 » = 350.
            Un prix dit (« à 8 euros ») va dans price_eur. Une correction remplace ce qui précède : « trois, non deux » veut dire 2.
            Tranche : pour un produit qui se vend à la tranche, un poids sans nombre d'unités (« 200 grammes de jambon ») ou des tranches (« quatre tranches »)
            donne slice = true, avec weight_g si un poids est dit. « Un jambon entier » donne slice = false.
            Paiement : paid = true seulement si la phrase dit que c'est payé.
            Ne devine pas : dans le doute, laisse le champ vide.
            """;
    }

    private static JsonArray Tools(IReadOnlyList<CatalogProduct> catalog)
    {
        var codes = new JsonArray(catalog.Select(p => (JsonNode)JsonValue.Create(p.Code)).ToArray());
        JsonObject Function(string name, string description, JsonObject parameters) => new()
        {
            ["type"] = "function",
            ["function"] = new JsonObject { ["name"] = name, ["description"] = description, ["parameters"] = parameters },
        };
        return
        [
            Function("get_stock", "Ce qui reste en stock, pour un produit ou pour tout le stock.", new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["product_code"] = new JsonObject { ["type"] = "string", ["description"] = "Code du produit ; absent pour tout le stock.", ["enum"] = codes.DeepClone() },
                },
            }),
            Function("draft_sale", "Prépare une vente, que l'utilisateur vérifiera avant de l'enregistrer.", new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["customer_token"] = new JsonObject { ["type"] = "string", ["description"] = "Jeton du client, par exemple [CLIENT_1]." },
                    ["paid"] = new JsonObject { ["type"] = "boolean", ["description"] = "Vrai seulement si la phrase dit que c'est payé." },
                    ["lines"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["properties"] = new JsonObject
                            {
                                ["product_code"] = new JsonObject { ["type"] = "string", ["enum"] = codes.DeepClone() },
                                ["quantity"] = new JsonObject { ["type"] = "integer", ["description"] = "Nombre d'unités." },
                                ["weight_g"] = new JsonObject { ["type"] = "number", ["description"] = "Poids voulu par unité, ou poids de la tranche, en grammes." },
                                ["price_eur"] = new JsonObject { ["type"] = "number", ["description"] = "Prix voulu par unité, en euros." },
                                ["slice"] = new JsonObject { ["type"] = "boolean", ["description"] = "Vente à la tranche." },
                            },
                            ["required"] = new JsonArray("product_code"),
                        },
                    },
                },
                ["required"] = new JsonArray("lines"),
            }),
            Function("not_understood", "La demande n'est ni une question de stock ni une vente, ou elle est inexploitable.", new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject { ["reason"] = new JsonObject { ["type"] = "string" } },
            }),
        ];
    }

    private static JsonObject Parse(string arguments)
    {
        try
        {
            return JsonNode.Parse(arguments) as JsonObject ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? ReadString(JsonObject o, string name) =>
        o[name] is JsonValue v && v.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s) ? s : null;

    private static decimal? ReadDecimal(JsonObject o, string name) => o[name] switch
    {
        JsonValue v when v.TryGetValue<decimal>(out var d) => d,
        JsonValue v when v.TryGetValue<string>(out var s)
            && decimal.TryParse(s.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var d) => d,
        _ => null,
    };

    private static bool? ReadBool(JsonObject o, string name) =>
        o[name] is JsonValue v && v.TryGetValue<bool>(out var b) ? b : null;
}
