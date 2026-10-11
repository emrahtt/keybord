using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace YazimAsistani.Core;

public static partial class TextNormalizer
{
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    private static readonly IReadOnlyDictionary<char, char> TurkishAsciiMap =
        new Dictionary<char, char>
        {
            ['ç'] = 'c',
            ['ğ'] = 'g',
            ['ı'] = 'i',
            ['ö'] = 'o',
            ['ş'] = 's',
            ['ü'] = 'u'
        };

    private static readonly IReadOnlyDictionary<string, string> MojibakeReplacements =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Ã§"] = "ç",
            ["Ã‡"] = "Ç",
            ["ÄŸ"] = "ğ",
            ["Äž"] = "Ğ",
            ["Ä±"] = "ı",
            ["Ä°"] = "İ",
            ["Ã¶"] = "ö",
            ["Ã–"] = "Ö",
            ["ÅŸ"] = "ş",
            ["Åž"] = "Ş",
            ["Ã¼"] = "ü",
            ["Ãœ"] = "Ü"
        };

    public static string NormalizeForLookup(string value, InputLanguage language)
    {
        ArgumentNullException.ThrowIfNull(value);

        var normalized = value.Trim().Normalize(NormalizationForm.FormC);
        normalized = language == InputLanguage.Turkish
            ? normalized.ToLower(TurkishCulture)
            : normalized.ToLowerInvariant();

        if (language != InputLanguage.Turkish)
        {
            return normalized;
        }

        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            builder.Append(TurkishAsciiMap.TryGetValue(character, out var ascii) ? ascii : character);
        }

        return builder.ToString();
    }

    public static string NormalizeUnicode(string value) =>
        RepairMojibake(value).Normalize(NormalizationForm.FormC);

    public static string ToUpper(string value, InputLanguage language) =>
        language == InputLanguage.Turkish
            ? value.ToUpper(TurkishCulture)
            : value.ToUpperInvariant();

    public static string ToLower(string value, InputLanguage language) =>
        language == InputLanguage.Turkish
            ? value.ToLower(TurkishCulture)
            : value.ToLowerInvariant();

    public static IReadOnlyList<string> Tokenize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return WordRegex()
            .Matches(text.Normalize(NormalizationForm.FormC))
            .Select(match => match.Value)
            .ToArray();
    }

    public static string RepairMojibake(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        foreach (var replacement in MojibakeReplacements)
        {
            value = value.Replace(replacement.Key, replacement.Value, StringComparison.Ordinal);
        }

        return value;
    }

    [GeneratedRegex(@"[\p{L}\p{Nd}]+(?:['’][\p{L}\p{Nd}]+)?", RegexOptions.CultureInvariant)]
    private static partial Regex WordRegex();
}

