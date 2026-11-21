using soft.square.net.WebApi.Modules.Taxonomies.Core.SchemeAggregate;

namespace soft.square.net.WebApi.Modules.Taxonomies.UseCases.Schemes;

/// <summary>A scheme in a list.</summary>
public sealed record SchemeSummaryDto(int Id, string Key, string Name, string? Description, string Language, int TermCount, DateTimeOffset UpdatedAt, int Version)
{
    public static SchemeSummaryDto From(TaxonomyScheme scheme) =>
        new(scheme.Id, scheme.Key, scheme.Name, scheme.Description, scheme.Language, scheme.Terms.Count, scheme.UpdatedAt, scheme.Version);
}

/// <summary>
/// A scheme with all its terms, in tree order (each term, then its narrower terms). <see cref="Version"/> goes back
/// with every change to the scheme or its terms; each change answers with the scheme as it is now.
/// </summary>
public sealed record SchemeDto(
    int Id,
    string Key,
    string Name,
    string? Description,
    string Language,
    IReadOnlyList<TermDto> Terms,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int Version)
{
    public static SchemeDto From(TaxonomyScheme scheme) =>
        new(scheme.Id, scheme.Key, scheme.Name, scheme.Description, scheme.Language,
            [.. scheme.InTreeOrder().Select(t => TermDto.From(scheme, t))],
            scheme.CreatedBy, scheme.CreatedAt, scheme.UpdatedAt, scheme.Version);
}

/// <summary>A term in its scheme's tree: where it is (parent, position, depth, path) and what it is.</summary>
public sealed record TermDto(
    int Id,
    int? ParentId,
    int Position,
    int Depth,
    string Name,
    string Slug,
    string? Code,
    string? Description,
    int? ConceptId,
    string? ConceptName,
    string Path,
    int ChildCount)
{
    public static TermDto From(TaxonomyScheme scheme, Term term)
    {
        var path = scheme.PathOf(term.Id);
        return new(term.Id, term.ParentId, term.Position, path.Count - 1, term.Name, term.Slug, term.Code, term.Description,
            term.ConceptId, term.ConceptName, SchemeJsonLd.PathText(path), scheme.Terms.Count(t => t.ParentId == term.Id));
    }
}

/// <summary>
/// A term named from elsewhere (a picker, content tagged with it): which scheme, its path from the top
/// (<see cref="PathNames"/>, and as text for <c>articleSection</c>) and the concept it stands for.
/// </summary>
public sealed record TermRefDto(
    int Id,
    string SchemeKey,
    string SchemeName,
    string Name,
    string Slug,
    string Path,
    IReadOnlyList<string> PathNames,
    int? ParentId,
    int? ConceptId,
    string? ConceptName)
{
    public static TermRefDto From(TaxonomyScheme scheme, Term term)
    {
        var path = scheme.PathOf(term.Id);
        return new(term.Id, scheme.Key, scheme.Name, term.Name, term.Slug, SchemeJsonLd.PathText(path),
            [.. path.Select(t => t.Name)], term.ParentId, term.ConceptId, term.ConceptName);
    }
}

/// <summary>A scheme or a term as JSON-LD, ready to embed.</summary>
public sealed record JsonLdDto(string Name, Dictionary<string, object?> JsonLd);

/// <summary>Small helpers the scheme use cases share.</summary>
public static class SchemeResults
{
    public static string NotFoundMessage(string key) => $"Scheme '{key}' was not found.";

    public static string StaleMessage(string key, int current, int read) =>
        $"Scheme '{key}' is at version {current}, not {read}; reload it and try again.";
}
