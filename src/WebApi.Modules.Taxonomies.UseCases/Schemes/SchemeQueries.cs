using Microsoft.Extensions.Options;
using soft.square.net.WebApi.Modules.Taxonomies.Core.SchemeAggregate;
using soft.square.net.WebApi.Modules.Taxonomies.Core.SchemeAggregate.Specifications;

namespace soft.square.net.WebApi.Modules.Taxonomies.UseCases.Schemes;

/// <summary>The tenant's schemes, by name.</summary>
public sealed record ListSchemesQuery : IQuery<Result<IReadOnlyList<SchemeSummaryDto>>>;

public sealed class ListSchemesHandler(IReadRepository<TaxonomyScheme> schemes) : IQueryHandler<ListSchemesQuery, Result<IReadOnlyList<SchemeSummaryDto>>>
{
    public async ValueTask<Result<IReadOnlyList<SchemeSummaryDto>>> Handle(ListSchemesQuery query, CancellationToken ct) =>
        (await schemes.ListAsync(new SchemesSpec(), ct)).Select(SchemeSummaryDto.From).ToList();
}

/// <summary>A scheme with its whole tree.</summary>
public sealed record GetSchemeQuery(string Key) : IQuery<Result<SchemeDto>>;

public sealed class GetSchemeHandler(IReadRepository<TaxonomyScheme> schemes) : IQueryHandler<GetSchemeQuery, Result<SchemeDto>>
{
    public async ValueTask<Result<SchemeDto>> Handle(GetSchemeQuery query, CancellationToken ct)
    {
        var scheme = await schemes.FirstOrDefaultAsync(new SchemeReadSpec(query.Key), ct);
        return scheme is null ? Result<SchemeDto>.NotFound(SchemeResults.NotFoundMessage(query.Key)) : SchemeDto.From(scheme);
    }
}

/// <summary>A scheme as a schema.org DefinedTermSet with all its terms.</summary>
public sealed record GetSchemeJsonLdQuery(string Key) : IQuery<Result<JsonLdDto>>;

public sealed class GetSchemeJsonLdHandler(IReadRepository<TaxonomyScheme> schemes) : IQueryHandler<GetSchemeJsonLdQuery, Result<JsonLdDto>>
{
    public async ValueTask<Result<JsonLdDto>> Handle(GetSchemeJsonLdQuery query, CancellationToken ct)
    {
        var scheme = await schemes.FirstOrDefaultAsync(new SchemeReadSpec(query.Key), ct);
        return scheme is null ? Result<JsonLdDto>.NotFound(SchemeResults.NotFoundMessage(query.Key)) : new JsonLdDto(scheme.Name, SchemeJsonLd.For(scheme));
    }
}

/// <summary>
/// Terms whose name, slug, code or linked concept name contains <paramref name="Search"/> (case, accents and Arabic
/// letter forms ignored), in one scheme or all, in tree order, at most <paramref name="Limit"/>. Without a search, the
/// first terms in tree order: what a picker shows before anything is typed.
/// </summary>
public sealed record SearchTermsQuery(string? Search = null, string? Scheme = null, int Limit = 50) : IQuery<Result<IReadOnlyList<TermRefDto>>>;

public sealed class SearchTermsHandler(IReadRepository<TaxonomyScheme> schemes, IOptions<TaxonomiesOptions> options)
    : IQueryHandler<SearchTermsQuery, Result<IReadOnlyList<TermRefDto>>>
{
    public async ValueTask<Result<IReadOnlyList<TermRefDto>>> Handle(SearchTermsQuery query, CancellationToken ct)
    {
        if (query.Limit < 1 || query.Limit > options.Value.MaxPageSize)
            return Result<IReadOnlyList<TermRefDto>>.Invalid(new ValidationError(nameof(query.Limit), $"The limit is 1 to {options.Value.MaxPageSize}."));

        var key = TextKey.For(query.Search);
        var found = new List<TermRefDto>();
        foreach (var scheme in await schemes.ListAsync(new SchemesSpec(query.Scheme), ct))
            foreach (var term in scheme.InTreeOrder())
            {
                if (key.Length > 0 && !Matches(term, key)) continue;
                found.Add(TermRefDto.From(scheme, term));
                if (found.Count == query.Limit) return found;
            }
        return found;
    }

    private static bool Matches(Term term, string key) =>
        TextKey.For(term.Name).Contains(key, StringComparison.Ordinal)
        || TextKey.For(term.Slug).Contains(key, StringComparison.Ordinal)
        || (term.Code is { } code && TextKey.For(code).Contains(key, StringComparison.Ordinal))
        || (term.ConceptName is { } concept && TextKey.For(concept).Contains(key, StringComparison.Ordinal));
}

/// <summary>The terms with these ids, in the order asked; unknown ids (removed terms) are left out.</summary>
public sealed record LookupTermsQuery(IReadOnlyList<int> Ids) : IQuery<Result<IReadOnlyList<TermRefDto>>>;

public sealed class LookupTermsHandler(IReadRepository<TaxonomyScheme> schemes, IOptions<TaxonomiesOptions> options)
    : IQueryHandler<LookupTermsQuery, Result<IReadOnlyList<TermRefDto>>>
{
    public async ValueTask<Result<IReadOnlyList<TermRefDto>>> Handle(LookupTermsQuery query, CancellationToken ct)
    {
        var ids = query.Ids.Distinct().ToList();
        if (ids.Count > options.Value.MaxPageSize)
            return Result<IReadOnlyList<TermRefDto>>.Invalid(new ValidationError(nameof(query.Ids), $"Look up at most {options.Value.MaxPageSize} ids at a time."));
        if (ids.Count == 0) return Result<IReadOnlyList<TermRefDto>>.Success([]);

        var found = new Dictionary<int, TermRefDto>();
        foreach (var scheme in await schemes.ListAsync(new SchemesWithTermsSpec(ids), ct))
            foreach (var term in scheme.Terms.Where(t => ids.Contains(t.Id)))
                found[term.Id] = TermRefDto.From(scheme, term);
        return ids.Where(found.ContainsKey).Select(id => found[id]).ToList();
    }
}

/// <summary>A term as a schema.org DefinedTerm in its DefinedTermSet: what a page tagged with it embeds as <c>about</c>.</summary>
public sealed record GetTermJsonLdQuery(int Id) : IQuery<Result<JsonLdDto>>;

public sealed class GetTermJsonLdHandler(IReadRepository<TaxonomyScheme> schemes) : IQueryHandler<GetTermJsonLdQuery, Result<JsonLdDto>>
{
    public async ValueTask<Result<JsonLdDto>> Handle(GetTermJsonLdQuery query, CancellationToken ct)
    {
        var scheme = await schemes.FirstOrDefaultAsync(new SchemesWithTermsSpec([query.Id]), ct);
        if (scheme is null) return Result<JsonLdDto>.NotFound($"Term {query.Id} was not found.");
        var term = scheme.Find(query.Id);
        return new JsonLdDto(SchemeJsonLd.PathText(scheme.PathOf(term.Id)), SchemeJsonLd.Term(scheme, term, withContext: true));
    }
}
