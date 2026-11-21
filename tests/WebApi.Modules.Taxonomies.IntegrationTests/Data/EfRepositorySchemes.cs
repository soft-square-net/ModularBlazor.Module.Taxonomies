using Microsoft.EntityFrameworkCore;
using soft.square.net.WebApi.Data;
using soft.square.net.WebApi.Modules.Taxonomies.Core.SchemeAggregate;
using soft.square.net.WebApi.Modules.Taxonomies.Core.SchemeAggregate.Specifications;

namespace soft.square.net.WebApi.Modules.Taxonomies.IntegrationTests.Data;

/// <summary>The module's mapping and specifications through the platform's repository, on a real database.</summary>
public sealed class EfRepositorySchemes : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 21, 9, 0, 0, TimeSpan.Zero);
    private readonly TaxonomiesDatabase _database = new();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _database.Dispose();

    private async Task<TaxonomyScheme> CreateAsync(string key = "sections", string tenant = "acme")
    {
        await using var context = _database.For(tenant);
        return await new EfRepository<TaxonomyScheme>(context).AddAsync(TaxonomyScheme.Create(key, new SchemeValues("Sections"), "user-1", Now), Ct);
    }

    /// <summary>One change, as one request makes it: load tracked, change, save.</summary>
    private async Task<TaxonomyScheme> ChangeAsync(Action<TaxonomyScheme> change, string key = "sections")
    {
        await using var context = _database.For("acme");
        var schemes = new EfRepository<TaxonomyScheme>(context);
        var scheme = (await schemes.FirstOrDefaultAsync(new SchemeByKeySpec(key), Ct))!;
        change(scheme);
        await schemes.UpdateAsync(scheme, Ct);
        return scheme;
    }

    private async Task<TaxonomyScheme> ReadAsync(string key = "sections")
    {
        await using var context = _database.For("acme");
        return (await new EfRepository<TaxonomyScheme>(context).FirstOrDefaultAsync(new SchemeReadSpec(key), Ct))!;
    }

    [Fact]
    public async Task BuildsATreeOneRequestAtATime()
    {
        await CreateAsync();
        var world = (await ChangeAsync(s => s.AddTerm(new TermValues("World"), null, Now))).Terms.Single();
        await ChangeAsync(s => s.AddTerm(new TermValues("Sports"), null, Now));
        var europe = (await ChangeAsync(s => s.AddTerm(new TermValues("Europe"), world.Id, Now))).Terms.Single(t => t.Name == "Europe");
        await ChangeAsync(s => s.AddTerm(new TermValues("France"), europe.Id, Now));

        await ChangeAsync(s => s.MoveTerm(europe.Id, null, 0, Now));
        var scheme = await ReadAsync();
        scheme.InTreeOrder().Select(t => t.Name).ShouldBe(["Europe", "France", "World", "Sports"]);
        scheme.Version.ShouldBe(5);

        await ChangeAsync(s => s.RemoveTerm(s.Terms.Single(t => t.Name == "France").Id, Now));
        await using var context = _database.For("acme");
        (await context.Set<Term>().CountAsync(Ct)).ShouldBe(3);
    }

    [Fact]
    public async Task StoresSchemesForTheirTenantOnly()
    {
        await CreateAsync(tenant: "acme");
        await CreateAsync(tenant: "globex"); // the same key in another tenant is fine

        await using var globex = _database.For("globex");
        (await new EfRepository<TaxonomyScheme>(globex).CountAsync(Ct)).ShouldBe(1);
        await Should.ThrowAsync<DbUpdateException>(() => CreateAsync(tenant: "acme"));
    }

    [Fact]
    public async Task FindsTheSchemesHoldingTerms()
    {
        await CreateAsync("sections");
        await CreateAsync("genres");
        var news = (await ChangeAsync(s => s.AddTerm(new TermValues("News"), null, Now), "genres")).Terms.Single();
        await ChangeAsync(s => s.AddTerm(new TermValues("World"), null, Now));

        await using var context = _database.For("acme");
        var found = await new EfRepository<TaxonomyScheme>(context).ListAsync(new SchemesWithTermsSpec([news.Id]), Ct);
        found.Single().Key.ShouldBe("genres");
        found.Single().Terms.Count.ShouldBe(1);
    }

    [Fact]
    public async Task TheVersionIsAConcurrencyToken()
    {
        await CreateAsync();

        await using var first = _database.For("acme");
        await using var second = _database.For("acme");
        var mine = await new EfRepository<TaxonomyScheme>(first).FirstOrDefaultAsync(new SchemeByKeySpec("sections"), Ct);
        var theirs = await new EfRepository<TaxonomyScheme>(second).FirstOrDefaultAsync(new SchemeByKeySpec("sections"), Ct);

        mine!.AddTerm(new TermValues("Mine"), null, Now);
        await new EfRepository<TaxonomyScheme>(first).UpdateAsync(mine, Ct);

        theirs!.AddTerm(new TermValues("Theirs"), null, Now);
        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => new EfRepository<TaxonomyScheme>(second).UpdateAsync(theirs, Ct));
    }
}
