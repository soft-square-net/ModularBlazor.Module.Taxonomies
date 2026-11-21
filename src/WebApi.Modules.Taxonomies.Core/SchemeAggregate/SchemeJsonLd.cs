namespace soft.square.net.WebApi.Modules.Taxonomies.Core.SchemeAggregate;

/// <summary>
/// Schemes and terms as schema.org JSON-LD. A term is a <c>DefinedTerm</c> with its <c>termCode</c> and the
/// <c>DefinedTermSet</c> it belongs to; a page tagged with it embeds that as <c>about</c>, and its path as
/// <c>articleSection</c> text. The consumer adds <c>@id</c>/<c>url</c> when it has public URLs for them.
/// </summary>
public static class SchemeJsonLd
{
    /// <summary>The scheme with all its terms, in tree order.</summary>
    public static Dictionary<string, object?> For(TaxonomyScheme scheme)
    {
        var json = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "DefinedTermSet",
            ["name"] = scheme.Name
        };
        if (scheme.Description is { Length: > 0 } description) json["description"] = description;
        json["inLanguage"] = scheme.Language;
        json["identifier"] = scheme.Key;
        json["hasDefinedTerm"] = scheme.InTreeOrder().Select(t => Term(scheme, t, inSet: false)).ToList();
        return json;
    }

    /// <summary>One term, with the scheme it is in (by name) unless it is listed inside that scheme.</summary>
    public static Dictionary<string, object?> Term(TaxonomyScheme scheme, Term term, bool inSet = true, bool withContext = false)
    {
        var json = new Dictionary<string, object?>();
        if (withContext) json["@context"] = "https://schema.org";
        json["@type"] = "DefinedTerm";
        json["name"] = term.Name;
        json["termCode"] = term.Code ?? term.Slug;
        if (term.Description is { Length: > 0 } description) json["description"] = description;
        if (inSet) json["inDefinedTermSet"] = new Dictionary<string, object?> { ["@type"] = "DefinedTermSet", ["name"] = scheme.Name, ["identifier"] = scheme.Key };
        return json;
    }

    /// <summary>The path as text, the way <c>articleSection</c> takes it: "World › Middle East › Egypt".</summary>
    public static string PathText(IEnumerable<Term> path) => string.Join(" › ", path.Select(t => t.Name));
}
