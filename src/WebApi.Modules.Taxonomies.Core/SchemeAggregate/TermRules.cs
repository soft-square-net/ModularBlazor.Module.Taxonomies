using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace soft.square.net.WebApi.Modules.Taxonomies.Core.SchemeAggregate;

/// <summary>The rules a scheme's and its terms' values follow; each check returns the cleaned value or throws <see cref="DomainException"/>.</summary>
public static partial class TermRules
{
    public const int KeyMaxLength = 64;
    public const int NameMaxLength = 200;
    public const int SlugMaxLength = 120;
    public const int CodeMaxLength = 64;
    public const int DescriptionMaxLength = 1000;
    public const int LanguageMaxLength = 16;
    public const int MaxTerms = 2000;
    public const int MaxDepth = 8;

    [GeneratedRegex("^[a-z0-9]+([.-][a-z0-9]+)*$")]
    private static partial Regex KeyPattern();

    [GeneratedRegex(@"^[\p{Ll}\p{Lo}\p{Lm}\p{Nd}]+(-[\p{Ll}\p{Lo}\p{Lm}\p{Nd}]+)*$")]
    private static partial Regex SlugPattern();

    [GeneratedRegex("^[a-z]{2,3}(-[a-z0-9]{2,8})*$")]
    private static partial Regex LanguagePattern();

    /// <summary>A scheme key: lower-case ASCII words joined by "." or "-" (<c>sections</c>, <c>recipe.categories</c>).</summary>
    public static string CheckKey(string key)
    {
        var value = (key ?? "").Trim();
        if (value.Length is 0 or > KeyMaxLength || !KeyPattern().IsMatch(value))
            throw new DomainException($"A scheme key is lower-case letters and digits joined by \".\" or \"-\", at most {KeyMaxLength} characters ('{key}').");
        return value;
    }

    public static string CheckName(string name)
    {
        var value = (name ?? "").Trim().Normalize(NormalizationForm.FormC);
        if (value.Length is 0 or > NameMaxLength) throw new DomainException($"A name is 1 to {NameMaxLength} characters.");
        return value;
    }

    public static string CheckSlug(string slug)
    {
        var value = slug.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormC);
        if (value.Length is 0 or > SlugMaxLength || !SlugPattern().IsMatch(value))
            throw new DomainException($"A slug is lower-case letters (any script) and digits joined by single hyphens, at most {SlugMaxLength} characters ('{slug}').");
        return value;
    }

    /// <summary>
    /// A slug from a name: its letters (any script, lower case, accents and Arabic diacritics dropped) and digits, words
    /// joined by hyphens: "Middle East" gives middle-east, "الشرق الأوسط" gives الشرق-الاوسط. Nothing usable gives "term".
    /// </summary>
    public static string Slugify(string name)
    {
        var slug = new StringBuilder();
        foreach (var c in name.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark || c == 'ـ') continue; // accents, harakat, tatweel
            var lower = char.ToLowerInvariant(c);
            if (char.IsLetterOrDigit(lower) && !char.IsUpper(lower)) slug.Append(lower);
            else if (slug.Length > 0 && slug[^1] != '-') slug.Append('-');
        }
        var text = slug.ToString().Normalize(NormalizationForm.FormC).Trim('-');
        if (text.Length > SlugMaxLength) text = text[..SlugMaxLength].TrimEnd('-');
        return text.Length == 0 ? "term" : text;
    }

    /// <summary>The first of stem, stem-2, stem-3... not in <paramref name="taken"/>.</summary>
    public static string FreeSlug(string stem, IReadOnlySet<string> taken)
    {
        if (!taken.Contains(stem)) return stem;
        for (var i = 2; ; i++)
        {
            var suffix = $"-{i}";
            var slug = (stem.Length + suffix.Length > SlugMaxLength ? stem[..(SlugMaxLength - suffix.Length)] : stem) + suffix;
            if (!taken.Contains(slug)) return slug;
        }
    }

    public static string? CheckCode(string? code)
    {
        var value = code?.Trim();
        if (string.IsNullOrEmpty(value)) return null;
        if (value.Length > CodeMaxLength) throw new DomainException($"A term code is at most {CodeMaxLength} characters.");
        return value;
    }

    public static string? CheckDescription(string? description)
    {
        var value = description?.Trim();
        if (string.IsNullOrEmpty(value)) return null;
        if (value.Length > DescriptionMaxLength) throw new DomainException($"A description is at most {DescriptionMaxLength} characters.");
        return value;
    }

    public static string? CheckConceptName(string? name)
    {
        var value = name?.Trim();
        if (string.IsNullOrEmpty(value)) return null;
        return value.Length > NameMaxLength ? value[..NameMaxLength] : value;
    }

    public static string CheckLanguage(string language)
    {
        var value = language.Trim().ToLowerInvariant();
        if (value.Length > LanguageMaxLength || !LanguagePattern().IsMatch(value))
            throw new DomainException($"'{language}' is not a language tag (en, ar, en-gb).");
        return value;
    }
}
