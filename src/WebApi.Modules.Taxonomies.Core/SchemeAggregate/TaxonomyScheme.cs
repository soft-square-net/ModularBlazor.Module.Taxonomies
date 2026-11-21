namespace soft.square.net.WebApi.Modules.Taxonomies.Core.SchemeAggregate;

/// <summary>
/// A classification scheme (schema.org <c>DefinedTermSet</c>): a tree of terms editors classify content with, such as
/// a news site's sections (World › Middle East › Egypt) or a recipe site's categories. Each term has at most one
/// broader term, in the same scheme, and an order among its siblings. A term can point to a Keywords concept (by id,
/// with the concept's name kept as a snapshot) when it stands for one, so it carries that concept's names and links;
/// Taxonomies works without the Keywords module all the same. The scheme is the aggregate: every term change goes
/// through it and bumps its version.
/// </summary>
public sealed class TaxonomyScheme : AggregateRoot<int>, ITenantOwned
{
    private readonly List<Term> _terms = [];

    private TaxonomyScheme() { } // EF Core

    /// <summary>Set by the platform's TenantDbContext when the scheme is saved.</summary>
    public string TenantId { get; set; } = "";

    /// <summary>The stable key content and other modules use (<c>sections</c>, <c>genres</c>); never changes.</summary>
    public string Key { get; private set; } = "";

    public string Name { get; private set; } = "";
    public string? Description { get; private set; }

    /// <summary>The language the term names are written in (<c>en</c>, <c>ar</c>), for JSON-LD's <c>inLanguage</c>.</summary>
    public string Language { get; private set; } = "en";

    public string CreatedBy { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Bumped by every change, terms included; a concurrency token, so a stale edit fails instead of overwriting.</summary>
    public int Version { get; private set; }

    public IReadOnlyList<Term> Terms => _terms;

    public static TaxonomyScheme Create(string key, SchemeValues values, string createdBy, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(createdBy)) throw new DomainException("A scheme needs its author.");
        var scheme = new TaxonomyScheme { Key = TermRules.CheckKey(key), CreatedBy = createdBy, CreatedAt = now };
        scheme.Set(values, now);
        return scheme;
    }

    public void Edit(SchemeValues values, DateTimeOffset now)
    {
        Set(values, now);
        Touch(now);
    }

    /// <summary>Adds a term under <paramref name="parentId"/> (null: at the top), after its siblings. Without a slug, one is made from the name.</summary>
    public Term AddTerm(TermValues values, int? parentId, DateTimeOffset now)
    {
        if (_terms.Count >= TermRules.MaxTerms) throw new DomainException($"A scheme has at most {TermRules.MaxTerms} terms.");
        if (parentId is { } parent && Depth(parent) + 1 >= TermRules.MaxDepth)
            throw new DomainException($"Terms nest at most {TermRules.MaxDepth} levels deep.");
        if (parentId is { } p) Find(p);

        var term = new Term(parentId, Siblings(parentId).Count());
        term.Set(Checked(values, null), now);
        _terms.Add(term);
        Touch(now);
        return term;
    }

    public Term EditTerm(int termId, TermValues values, DateTimeOffset now)
    {
        var term = Find(termId);
        term.Set(Checked(values, term), now);
        Touch(now);
        return term;
    }

    /// <summary>
    /// Moves a term (with everything under it) under <paramref name="parentId"/> (null: the top) at
    /// <paramref name="position"/> among its new siblings (clamped). Not under itself or under one of its own terms.
    /// </summary>
    public void MoveTerm(int termId, int? parentId, int position, DateTimeOffset now)
    {
        var term = Find(termId);
        if (parentId is { } p)
        {
            Find(p);
            if (p == termId || Descendants(termId).Any(d => d.Id == p))
                throw new DomainException($"'{term.Name}' cannot go under itself or one of its own terms.");
            if (Depth(p) + 1 + Height(termId) > TermRules.MaxDepth)
                throw new DomainException($"Terms nest at most {TermRules.MaxDepth} levels deep.");
        }

        var oldParent = term.ParentId;
        var siblings = Siblings(parentId).Where(t => t != term).ToList();
        siblings.Insert(Math.Clamp(position, 0, siblings.Count), term);
        term.MoveTo(parentId);
        for (var i = 0; i < siblings.Count; i++) siblings[i].PlaceAt(i);
        if (oldParent != parentId) Renumber(oldParent);
        Touch(now);
    }

    /// <summary>Removes a term that has no narrower terms. Content tagged with it keeps the id (as a term that is gone).</summary>
    public void RemoveTerm(int termId, DateTimeOffset now)
    {
        var term = Find(termId);
        if (_terms.Any(t => t.ParentId == termId))
            throw new DomainException($"'{term.Name}' has narrower terms: move or remove them first.");
        _terms.Remove(term);
        Renumber(term.ParentId);
        Touch(now);
    }

    /// <summary>The term and its broader terms, from the top down (World, Middle East, Egypt).</summary>
    public IReadOnlyList<Term> PathOf(int termId)
    {
        var path = new List<Term>();
        for (Term? t = Find(termId); t is not null; t = t.ParentId is { } p ? _terms.FirstOrDefault(x => x.Id == p) : null)
            path.Insert(0, t);
        return path;
    }

    /// <summary>The terms in tree order: each term, then its narrower terms, siblings by position.</summary>
    public IReadOnlyList<Term> InTreeOrder()
    {
        var ordered = new List<Term>(_terms.Count);
        void Visit(int? parent)
        {
            foreach (var t in Siblings(parent))
            {
                ordered.Add(t);
                Visit(t.Id);
            }
        }
        Visit(null);
        return ordered;
    }

    public Term Find(int termId) =>
        _terms.FirstOrDefault(t => t.Id == termId) ?? throw new DomainException($"Term {termId} is not in scheme '{Key}'.");

    private IEnumerable<Term> Siblings(int? parentId) => _terms.Where(t => t.ParentId == parentId).OrderBy(t => t.Position).ThenBy(t => t.Id);

    private IEnumerable<Term> Descendants(int termId)
    {
        foreach (var child in _terms.Where(t => t.ParentId == termId))
        {
            yield return child;
            foreach (var d in Descendants(child.Id)) yield return d;
        }
    }

    /// <summary>0 for a top term.</summary>
    private int Depth(int termId) => PathOf(termId).Count - 1;

    /// <summary>1 for a term without narrower terms.</summary>
    private int Height(int termId) => 1 + _terms.Where(t => t.ParentId == termId).Select(t => Height(t.Id)).DefaultIfEmpty(0).Max();

    private void Renumber(int? parentId)
    {
        var i = 0;
        foreach (var t in Siblings(parentId).ToList()) t.PlaceAt(i++);
    }

    /// <summary>The values cleaned, with a slug unique in the scheme (made from the name, numbered, when none is given).</summary>
    private TermValues Checked(TermValues values, Term? self)
    {
        var name = TermRules.CheckName(values.Name);
        var taken = _terms.Where(t => t != self).Select(t => t.Slug).ToHashSet(StringComparer.Ordinal);
        string slug;
        if (values.Slug is { Length: > 0 } given)
        {
            slug = TermRules.CheckSlug(given);
            if (taken.Contains(slug)) throw new DomainException($"The slug '{slug}' is taken in scheme '{Key}'.");
        }
        else slug = self is not null && TermRules.Slugify(name) == TermRules.Slugify(self.Name) ? self.Slug : TermRules.FreeSlug(TermRules.Slugify(name), taken);

        return values with
        {
            Name = name,
            Slug = slug,
            Code = TermRules.CheckCode(values.Code),
            Description = TermRules.CheckDescription(values.Description),
            ConceptId = values.ConceptId is { } id && id <= 0 ? throw new DomainException("A concept id is a positive number.") : values.ConceptId,
            ConceptName = values.ConceptId is null ? null : TermRules.CheckConceptName(values.ConceptName)
        };
    }

    private void Set(SchemeValues values, DateTimeOffset now)
    {
        Name = TermRules.CheckName(values.Name);
        Description = TermRules.CheckDescription(values.Description);
        Language = TermRules.CheckLanguage(values.Language ?? "en");
        UpdatedAt = now;
    }

    private void Touch(DateTimeOffset now)
    {
        UpdatedAt = now;
        Version++;
    }
}

/// <summary>One term of a scheme (schema.org <c>DefinedTerm</c>).</summary>
public sealed class Term
{
    private Term() { } // EF Core

    internal Term(int? parentId, int position)
    {
        ParentId = parentId;
        Position = position;
    }

    public int Id { get; private set; }

    /// <summary>The broader term, in the same scheme; null at the top.</summary>
    public int? ParentId { get; private set; }

    /// <summary>The order among its siblings, from 0.</summary>
    public int Position { get; private set; }

    public string Name { get; private set; } = "";

    /// <summary>URL-safe, unique in the scheme (<c>middle-east</c>, <c>الشرق-الأوسط</c>).</summary>
    public string Slug { get; private set; } = "";

    /// <summary>schema.org <c>termCode</c> when it has one of its own (an IPTC code, an internal number); else the slug is used.</summary>
    public string? Code { get; private set; }

    public string? Description { get; private set; }

    /// <summary>The Keywords concept this term stands for, if any.</summary>
    public int? ConceptId { get; private set; }

    /// <summary>The concept's name when it was linked, shown without asking Keywords.</summary>
    public string? ConceptName { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    internal void Set(TermValues values, DateTimeOffset now)
    {
        Name = values.Name;
        Slug = values.Slug!;
        Code = values.Code;
        Description = values.Description;
        ConceptId = values.ConceptId;
        ConceptName = values.ConceptName;
        UpdatedAt = now;
    }

    internal void MoveTo(int? parentId) => ParentId = parentId;

    internal void PlaceAt(int position) => Position = position;
}

/// <summary>What an editor sets on a scheme.</summary>
public sealed record SchemeValues(string Name, string? Description = null, string? Language = null);

/// <summary>What an editor sets on a term. No slug: made from the name.</summary>
public sealed record TermValues(
    string Name,
    string? Slug = null,
    string? Code = null,
    string? Description = null,
    int? ConceptId = null,
    string? ConceptName = null);
