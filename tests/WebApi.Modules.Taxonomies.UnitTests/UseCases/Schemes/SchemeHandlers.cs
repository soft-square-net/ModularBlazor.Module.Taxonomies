using Ardalis.Specification;
using Microsoft.Extensions.Options;
using soft.square.net.WebApi.Modules.Taxonomies.Core.SchemeAggregate;
using soft.square.net.WebApi.Modules.Taxonomies.UnitTests.Core.SchemeAggregate;
using soft.square.net.WebApi.Modules.Taxonomies.UseCases;
using soft.square.net.WebApi.Modules.Taxonomies.UseCases.Schemes;

namespace soft.square.net.WebApi.Modules.Taxonomies.UnitTests.UseCases.Schemes;

/// <summary>The scheme use cases over substituted repositories and platform contracts: no database, no host.</summary>
public class SchemeHandlers
{
    private readonly IRepository<TaxonomyScheme> _schemes = Substitute.For<IRepository<TaxonomyScheme>>();
    private readonly IReadRepository<TaxonomyScheme> _reader = Substitute.For<IReadRepository<TaxonomyScheme>>();
    private readonly IAccessScopes _access = Substitute.For<IAccessScopes>();
    private readonly TestClock _clock = new();
    private readonly IOptions<TaxonomiesOptions> _options = Options.Create(new TaxonomiesOptions { MaxPageSize = 50 });

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private SchemeBuilder Stored()
    {
        var b = new SchemeBuilder();
        var world = b.Add("World");
        b.Add("الشرق الأوسط", world);
        b.Add("Sports");
        _schemes.FirstOrDefaultAsync(Arg.Any<ISpecification<TaxonomyScheme>>(), Arg.Any<CancellationToken>()).Returns(b.Scheme);
        _reader.ListAsync(Arg.Any<ISpecification<TaxonomyScheme>>(), Arg.Any<CancellationToken>()).Returns([b.Scheme]);
        return b;
    }

    [Fact]
    public async Task CreateNeedsASignedInCallerAndAFreeKey()
    {
        var handler = new CreateSchemeHandler(_schemes, _access, _clock);
        (await handler.Handle(new CreateSchemeCommand("sections", new SchemeValues("Sections")), Ct)).Status.ShouldBe(ResultStatus.Unauthorized);

        _access.UserId.Returns("user-1");
        _schemes.AnyAsync(Arg.Any<ISpecification<TaxonomyScheme>>(), Arg.Any<CancellationToken>()).Returns(true);
        (await handler.Handle(new CreateSchemeCommand("sections", new SchemeValues("Sections")), Ct)).Status.ShouldBe(ResultStatus.Conflict);
    }

    [Fact]
    public async Task ChangesCheckTheVersionFirst()
    {
        var b = Stored(); // version 3

        var result = await new SchemeChangeHandler(_schemes, _clock).Handle(new AddTermCommand("sections", 2, null, new TermValues("Late")), Ct);

        result.Status.ShouldBe(ResultStatus.Conflict);
        b.Scheme.Terms.Count.ShouldBe(3);
        await _schemes.DidNotReceiveWithAnyArgs().UpdateAsync(default!, Ct);
    }

    [Fact]
    public async Task AddAnswersWithTheWholeTree()
    {
        Stored();

        var result = await new SchemeChangeHandler(_schemes, _clock).Handle(new AddTermCommand("sections", 3, 1, new TermValues("Europe")), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Version.ShouldBe(4);
        result.Value.Terms.Select(t => t.Name).ShouldBe(["World", "الشرق الأوسط", "Europe", "Sports"]);
        await _schemes.Received(1).UpdateAsync(Arg.Any<TaxonomyScheme>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteRefusesASchemeWithTerms()
    {
        Stored();

        var result = await new DeleteSchemeHandler(_schemes).Handle(new DeleteSchemeCommand("sections", 3), Ct);

        result.Status.ShouldBe(ResultStatus.Conflict);
        await _schemes.DidNotReceiveWithAnyArgs().DeleteAsync(default!, Ct);
    }

    [Fact]
    public async Task SearchFoldsArabicAndKeepsTreeOrder()
    {
        Stored();
        var handler = new SearchTermsHandler(_reader, _options);

        var arabic = await handler.Handle(new SearchTermsQuery("الاوسط"), Ct);
        arabic.Value.Single().Path.ShouldBe("World › الشرق الأوسط");
        arabic.Value.Single().PathNames.ShouldBe(["World", "الشرق الأوسط"]);

        (await handler.Handle(new SearchTermsQuery(null, null, 2), Ct)).Value.Select(t => t.Name).ShouldBe(["World", "الشرق الأوسط"]);
        (await handler.Handle(new SearchTermsQuery(null, null, 51), Ct)).Status.ShouldBe(ResultStatus.Invalid);
    }

    [Fact]
    public async Task LookupKeepsTheOrderAskedAndSkipsRemovedTerms()
    {
        Stored();

        var result = await new LookupTermsHandler(_reader, _options).Handle(new LookupTermsQuery([3, 42, 1]), Ct);

        result.Value.Select(t => t.Name).ShouldBe(["Sports", "World"]);
        result.Value[0].SchemeKey.ShouldBe("sections");
    }
}
