using System.Globalization;
using System.Text;

namespace Butcher.Api.Application.Assistant;

/// <summary>
/// Code phonétique d'un mot français, pour comparer des noms à l'oreille plutôt qu'à l'orthographe
/// (spike assistant vocal, étape 2). « Moreau », « moraux » et « moro » partagent un code ;
/// « Lefèvre » et « Lefebvre » aussi. « Martin » (son « in ») et « Martine » (son « ine ») non :
/// ce ne sont pas les mêmes personnes.
/// </summary>
/// <remarks>
/// Règles volontairement simples, écrites pour des noms propres dictés. Les voyelles nasales
/// deviennent des chiffres (1 = in, 2 = an, 3 = on) avant la chute du « e » final, pour que
/// « martin » et « martine » restent distincts.
/// </remarks>
public static class FrenchPhonetic
{
    public static string Normalize(string text)
    {
        var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            builder.Append(c == 'œ' ? "oe" : c == 'æ' ? "ae" : c.ToString());
        }
        return builder.ToString();
    }

    public static string Encode(string word)
    {
        var w = Normalize(word).Where(char.IsLetter).Aggregate(new StringBuilder(), (b, c) => b.Append(c)).ToString();
        if (w.Length == 0)
            return "";

        // Consonnes et graphies composées.
        w = w.Replace("sch", "#").Replace("ch", "#").Replace("ph", "f").Replace("qu", "k").Replace("ck", "k")
             .Replace("gu", "g").Replace("bv", "v").Replace("w", "v").Replace("y", "i").Replace("h", "");
        w = ReplaceBeforeFront(w, 'c', 's', 'k');
        w = ReplaceBeforeFront(w, 'g', 'j', 'g');

        // Voyelles composées.
        w = w.Replace("eau", "o").Replace("au", "o").Replace("ou", "u").Replace("ai", "e").Replace("ei", "e");

        w = Nasals(w);

        // Lettres doublées, « s » entre voyelles, finales muettes.
        w = CollapseDoubles(w);
        w = SoftS(w);
        if (w.Length > 1 && "sxtdz".Contains(w[^1]))
            w = w[..^1];
        if (w.Length > 1 && w[^1] == 'e')
            w = w[..^1];

        return CollapseDoubles(w);
    }

    private static string ReplaceBeforeFront(string w, char letter, char soft, char hard)
    {
        var chars = w.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] != letter)
                continue;
            var next = i + 1 < chars.Length ? chars[i + 1] : ' ';
            chars[i] = next is 'e' or 'i' ? soft : hard;
        }
        return new string(chars);
    }

    private static string Nasals(string w)
    {
        var builder = new StringBuilder();
        var i = 0;
        while (i < w.Length)
        {
            var nasal = TryNasal(w, i, out var length);
            if (nasal is not null)
            {
                builder.Append(nasal);
                i += length;
            }
            else
            {
                builder.Append(w[i]);
                i++;
            }
        }
        return builder.ToString();
    }

    /// <summary>Une voyelle suivie de n/m est nasale si la lettre d'après n'est ni une voyelle, ni n/m.</summary>
    private static string? TryNasal(string w, int i, out int length)
    {
        (string Spelling, string Code)[] groups =
        [
            ("ain", "1"), ("ein", "1"), ("ean", "2"), ("in", "1"), ("im", "1"), ("un", "1"),
            ("an", "2"), ("am", "2"), ("en", "2"), ("em", "2"), ("on", "3"), ("om", "3"),
        ];
        foreach (var (spelling, code) in groups)
        {
            if (string.CompareOrdinal(w, i, spelling, 0, spelling.Length) != 0)
                continue;
            var after = i + spelling.Length < w.Length ? w[i + spelling.Length] : ' ';
            if (IsVowel(after) || after is 'n' or 'm')
                continue;
            length = spelling.Length;
            return code;
        }
        length = 0;
        return null;
    }

    private static string CollapseDoubles(string w)
    {
        var builder = new StringBuilder(w.Length);
        foreach (var c in w)
            if (builder.Length == 0 || builder[^1] != c)
                builder.Append(c);
        return builder.ToString();
    }

    private static string SoftS(string w)
    {
        var chars = w.ToCharArray();
        for (var i = 1; i < chars.Length - 1; i++)
            if (chars[i] == 's' && IsVowel(chars[i - 1]) && IsVowel(chars[i + 1]))
                chars[i] = 'z';
        return new string(chars);
    }

    private static bool IsVowel(char c) => c is 'a' or 'e' or 'i' or 'o' or 'u';
}
