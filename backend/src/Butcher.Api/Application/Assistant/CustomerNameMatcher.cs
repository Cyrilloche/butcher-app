using System.Text;
using System.Text.RegularExpressions;

namespace Butcher.Api.Application.Assistant;

public sealed record CustomerRef(int Id, string LastName, string? FirstName);

public enum MentionStatus
{
    /// <summary>Un seul client correspond, sans doute possible.</summary>
    Matched,

    /// <summary>Plusieurs clients possibles : aucun n'est choisi, il se choisit à l'écran.</summary>
    Ambiguous,

    /// <summary>Un nom a été cité mais ne correspond à aucun client : il est retiré (D-08).</summary>
    Unknown,
}

/// <param name="Heard">Les mots entendus. Reste dans le backend : n'est jamais envoyé au LLM.</param>
public sealed record CustomerMention(string Token, MentionStatus Status, int? CustomerId, string Heard);

/// <param name="Text">Le texte à envoyer au LLM : aucun nom de client n'y figure.</param>
public sealed record PseudonymizedText(string Text, IReadOnlyList<CustomerMention> Mentions);

/// <summary>
/// Retrouve les clients cités dans une phrase transcrite et les remplace par des jetons avant
/// l'appel au LLM (spike assistant vocal, étape 2 ; cadrage D-05, D-08).
/// </summary>
/// <remarks>
/// La comparaison se fait à l'oreille (<see cref="FrenchPhonetic"/>). Deux garde-fous tiennent
/// le critère « aucun mauvais client » : une correspondance approchée n'est cherchée qu'après une
/// civilité, une préposition, un article ou en début de phrase ; et quand deux clients se
/// ressemblent (Martin / Martine), on n'en choisit un que si le contexte le confirme.
/// </remarks>
public sealed class CustomerNameMatcher
{
    public const string UnknownToken = "[CLIENT_INCONNU]";

    private const double Exact = 1.0;
    private const double SameSound = 0.9;
    private const double CloseSound = 0.7;

    private static readonly HashSet<string> Civilities =
        ["madame", "mme", "monsieur", "m", "mr", "mademoiselle", "mlle"];

    private static readonly HashSet<string> Prepositions = ["a", "pour", "aux", "au", "chez"];

    private static readonly HashSet<string> Articles = ["la", "le", "les"];

    /// <summary>Jamais pris pour un nom : nombres, mots de la vente, mots courants des phrases dictées.</summary>
    private static readonly HashSet<string> CommonWords =
    [
        "un", "une", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf", "dix", "onze",
        "douze", "vingt", "trente", "quarante", "cinquante", "soixante", "cent", "cents", "mille", "demi",
        "saucisson", "saucissons", "jambon", "jambons", "terrine", "terrines", "tranche", "tranches",
        "gramme", "grammes", "kilo", "kilos", "livre", "livres", "euro", "euros", "stock",
        "vends", "vend", "vendu", "vendre", "mets", "met", "donne", "coupe", "pris", "prend", "annule",
        "paye", "payee", "payera", "paiera", "reste", "restent", "entier", "entame", "gros", "petit",
        "il", "elle", "ils", "elles", "on", "nous", "vous", "je", "j", "tu", "me", "moi", "toi", "lui", "eux",
        "de", "du", "des", "d", "l", "s", "n", "qu", "que", "quoi", "et", "ou", "en", "y", "ce", "ces",
        "ses", "son", "sa", "mes", "mon", "ma", "est", "ai", "as", "avons", "a", "combien", "encore",
        "environ", "semaine", "prochaine", "derniere", "dernier", "vente", "liquide", "chansons",
        "enfants", "non", "oui", "bon", "alors", "euh", "attends", "dis", "voir", "quelque", "chose",
    ];

    private readonly List<(CustomerRef Customer, FormKind Kind, string Normalized, string Code)> _forms;

    public CustomerNameMatcher(IEnumerable<CustomerRef> customers)
    {
        _forms = [];
        foreach (var customer in customers)
        {
            AddForm(customer, FormKind.Last, customer.LastName);
            if (!string.IsNullOrWhiteSpace(customer.FirstName))
            {
                AddForm(customer, FormKind.First, customer.FirstName);
                AddForm(customer, FormKind.Full, $"{customer.FirstName} {customer.LastName}");
                AddForm(customer, FormKind.Full, $"{customer.LastName} {customer.FirstName}");
            }
        }
    }

    private enum FormKind { Last, First, Full }

    private void AddForm(CustomerRef customer, FormKind kind, string value)
    {
        var joined = Join(value);
        _forms.Add((customer, kind, joined, FrenchPhonetic.Encode(joined)));
    }

    private static string Join(string value) =>
        string.Concat(Regex.Matches(FrenchPhonetic.Normalize(value), @"\p{L}+").Select(m => m.Value));

    public PseudonymizedText Pseudonymize(string text)
    {
        var words = Regex.Matches(text, @"\p{L}+").Select(m => new Word(m.Index, m.Length, m.Value,
            FrenchPhonetic.Normalize(m.Value))).ToList();
        var output = new StringBuilder();
        var mentions = new List<CustomerMention>();
        var cursor = 0;
        var matchedCount = 0;

        for (var i = 0; i < words.Count; i++)
        {
            var span = FindMention(text, words, i);
            if (span is null)
                continue;

            var (length, status, customerId) = span.Value;
            var first = i;
            // La civilité ou l'article qui précède part avec le nom : « à [CLIENT_1] », pas « à madame [CLIENT_1] ».
            if (first > 0 && (Civilities.Contains(words[first - 1].Normalized) || Articles.Contains(words[first - 1].Normalized)))
                first--;
            var last = words[i + length - 1];
            var start = words[first].Start;
            var end = last.Start + last.Length;

            var token = status == MentionStatus.Matched ? $"[CLIENT_{++matchedCount}]" : UnknownToken;
            output.Append(text, cursor, start - cursor).Append(token);
            cursor = end;
            mentions.Add(new CustomerMention(token, status, customerId, text[start..end]));
            i += length - 1;
        }

        output.Append(text, cursor, text.Length - cursor);
        return new PseudonymizedText(output.ToString(), mentions);
    }

    private (int Length, MentionStatus Status, int? CustomerId)? FindMention(string text, List<Word> words, int i)
    {
        var word = words[i];
        if (CommonWords.Contains(word.Normalized) || Civilities.Contains(word.Normalized)
            || Prepositions.Contains(word.Normalized) || Articles.Contains(word.Normalized))
            return null;

        var previous = i > 0 ? words[i - 1].Normalized : null;
        var afterCivility = previous is not null && Civilities.Contains(previous);
        var inContext = afterCivility || IsSentenceStart(text, words, i)
            || (previous is not null && (Prepositions.Contains(previous) || Articles.Contains(previous)));

        // Nom complet sur deux mots d'abord, puis un seul mot.
        for (var length = Math.Min(2, words.Count - i); length >= 1; length--)
        {
            var window = words.GetRange(i, length);
            if (window.Skip(1).Any(w => CommonWords.Contains(w.Normalized)))
                continue;
            var decision = Decide(string.Concat(window.Select(w => w.Normalized)), inContext, afterCivility);
            if (decision is not null)
                return (length, decision.Value.Status, decision.Value.CustomerId);
        }

        // Un nom cité mais inconnu : après une civilité, ou un mot en capitale après une préposition ou un article.
        var capitalized = char.IsUpper(word.Value[0]) && !IsSentenceStart(text, words, i);
        if (afterCivility || (inContext && previous is not null && capitalized))
        {
            var length = 1;
            if (i + 1 < words.Count && char.IsUpper(words[i + 1].Value[0]) && !CommonWords.Contains(words[i + 1].Normalized))
                length = 2;
            return (length, MentionStatus.Unknown, null);
        }
        return null;
    }

    private (MentionStatus Status, int? CustomerId)? Decide(string heard, bool inContext, bool afterCivility)
    {
        var heardCode = FrenchPhonetic.Encode(heard);
        var scored = _forms
            .Select(f => (f.Customer, f.Kind, Score: Score(heard, heardCode, f.Normalized, f.Code)))
            .Where(s => s.Score >= (inContext ? CloseSound : Exact))
            .GroupBy(s => s.Customer.Id)
            .Select(g => g.OrderByDescending(s => s.Score).First())
            .OrderByDescending(s => s.Score)
            .ToList();
        if (scored.Count == 0)
            return null;

        var best = scored[0];
        if (scored.Skip(1).Any(s => s.Score >= best.Score - 0.1))
            return (MentionStatus.Ambiguous, null);

        // Un autre client se prononce presque pareil (Martin / Martine) : la transcription a pu
        // confondre les deux. On ne choisit que si le contexte le confirme.
        var confusable = heardCode.Length >= 4 && _forms.Any(f =>
            f.Customer.Id != best.Customer.Id && f.Kind != FormKind.Full && Levenshtein(heardCode, f.Code) <= 2);
        if (confusable)
        {
            var confirmed = best.Score == Exact
                && (best.Kind == FormKind.Full || (best.Kind == FormKind.Last && afterCivility));
            if (!confirmed)
                return (MentionStatus.Ambiguous, null);
        }
        return (MentionStatus.Matched, best.Customer.Id);
    }

    private static double Score(string heard, string heardCode, string form, string formCode)
    {
        if (heard == form)
            return Exact;
        if (heardCode == formCode)
            return SameSound;
        return Math.Min(heardCode.Length, formCode.Length) >= 4 && Levenshtein(heardCode, formCode) == 1
            ? CloseSound
            : 0;
    }

    private static bool IsSentenceStart(string text, List<Word> words, int i)
    {
        if (i == 0)
            return true;
        var between = text[(words[i - 1].Start + words[i - 1].Length)..words[i].Start];
        return between.IndexOfAny(['.', '!', '?', ',']) >= 0;
    }

    private static int Levenshtein(string a, string b)
    {
        var previous = Enumerable.Range(0, b.Length + 1).ToArray();
        for (var i = 1; i <= a.Length; i++)
        {
            var current = new int[b.Length + 1];
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            previous = current;
        }
        return previous[b.Length];
    }

    private sealed record Word(int Start, int Length, string Value, string Normalized);
}
