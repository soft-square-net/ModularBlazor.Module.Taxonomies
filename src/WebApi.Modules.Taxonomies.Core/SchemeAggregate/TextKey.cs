using System.Globalization;
using System.Text;

namespace soft.square.net.WebApi.Modules.Taxonomies.Core.SchemeAggregate;

/// <summary>
/// The search key of a term name or a query: the text folded so that the ways people type one name match each other,
/// while the name itself keeps its original form. It lowers case, drops accents and Arabic diacritics (harakat,
/// shadda, sukun, superscript alef) and tatweel, writes every alef form (أ إ آ ٱ) as ا and alef maqsura (ى) as ya (ي),
/// turns Arabic-Indic digits into 0-9 and collapses whitespace and punctuation to single spaces. Ta marbuta and ha are
/// kept apart: folding them would merge different words.
/// </summary>
public static class TextKey
{
    public static string For(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var key = new StringBuilder(decomposed.Length);
        var space = false;
        foreach (var raw in decomposed)
        {
            var c = raw;
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark) continue; // accents, harakat
            if (c == 'ـ') continue; // tatweel
            c = c switch
            {
                'أ' or 'إ' or 'آ' or 'ٱ' => 'ا', // أ إ آ ٱ → ا
                'ى' => 'ي', // ى → ي
                >= '٠' and <= '٩' => (char)('0' + (c - '٠')), // Arabic-Indic digits
                >= '۰' and <= '۹' => (char)('0' + (c - '۰')), // Persian digits
                _ => c
            };
            if (char.IsLetterOrDigit(c))
            {
                if (space && key.Length > 0) key.Append(' ');
                space = false;
                key.Append(char.ToLowerInvariant(c));
            }
            else space = true;
        }
        return key.ToString().Normalize(NormalizationForm.FormC);
    }
}
