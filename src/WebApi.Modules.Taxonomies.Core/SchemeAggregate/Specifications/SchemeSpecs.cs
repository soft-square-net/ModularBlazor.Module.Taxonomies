namespace soft.square.net.WebApi.Modules.Taxonomies.Core.SchemeAggregate.Specifications;

/// <summary>One scheme with its terms, tracked (for changing it).</summary>
public sealed class SchemeByKeySpec : SingleResultSpecification<TaxonomyScheme>
{
    public SchemeByKeySpec(string key) => Query.Where(s => s.Key == key).Include(s => s.Terms);
}

/// <summary>One scheme with its terms, untracked (for reading it).</summary>
public sealed class SchemeReadSpec : SingleResultSpecification<TaxonomyScheme>
{
    public SchemeReadSpec(string key) => Query.Where(s => s.Key == key).Include(s => s.Terms).AsNoTracking();
}

/// <summary>Every scheme by name, with its terms, untracked (schemes are few; their terms are what lists count).</summary>
public sealed class SchemesSpec : Specification<TaxonomyScheme>
{
    public SchemesSpec(string? key = null)
    {
        if (!string.IsNullOrWhiteSpace(key)) Query.Where(s => s.Key == key);
        Query.OrderBy(s => s.Name).ThenBy(s => s.Id).Include(s => s.Terms).AsNoTracking();
    }
}

/// <summary>The schemes holding any of these term ids, with their terms, untracked.</summary>
public sealed class SchemesWithTermsSpec : Specification<TaxonomyScheme>
{
    public SchemesWithTermsSpec(IReadOnlyCollection<int> termIds) =>
        Query.Where(s => s.Terms.Any(t => termIds.Contains(t.Id))).Include(s => s.Terms).AsNoTracking();
}
