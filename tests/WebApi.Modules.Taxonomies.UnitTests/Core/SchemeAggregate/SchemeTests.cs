using soft.square.net.WebApi.Modules.Taxonomies.Core.SchemeAggregate;

namespace soft.square.net.WebApi.Modules.Taxonomies.UnitTests.Core.SchemeAggregate;

/// <summary>A scheme as the database would hand it back: every term has its id (the database gives them on save).</summary>
public sealed class SchemeBuilder
{
    private int _next = 1;

    public TaxonomyScheme Scheme { get; } = TaxonomyScheme.Create("sections", new SchemeValues("Sections", null, "en"), "user-1", TestClock.Start);

    public Term Add(string name, Term? parent = null, string? slug = null)
    {
        var term = Scheme.AddTerm(new TermValues(name, slug), parent?.Id, TestClock.Start);
        typeof(Term).GetProperty(nameof(Term.Id))!.SetValue(term, _next++);
        return term;
    }

    public string Tree() => string.Join(" | ", Scheme.InTreeOrder().Select(t => new string('-', Scheme.PathOf(t.Id).Count - 1) + t.Name));
}

public class SchemeTests
{
    private static readonly DateTimeOffset Now = TestClock.Start;

    [Theory]
    [InlineData("Sections")]
    [InlineData("has space")]
    [InlineData(".dot")]
    public void RejectsBadKeys(string key) =>
        Should.Throw<DomainException>(() => TaxonomyScheme.Create(key, new SchemeValues("Name"), "user-1", Now));

    [Fact]
    public void AddsTermsInOrderUnderTheirParents()
    {
        var b = new SchemeBuilder();
        var world = b.Add("World");
        b.Add("Sports");
        var mideast = b.Add("Middle East", world);
        b.Add("Egypt", mideast);
        b.Add("Europe", world);

        b.Tree().ShouldBe("World | -Middle East | --Egypt | -Europe | Sports");
        b.Scheme.PathOf(4).Select(t => t.Name).ShouldBe(["World", "Middle East", "Egypt"]);
        SchemeJsonLd.PathText(b.Scheme.PathOf(4)).ShouldBe("World › Middle East › Egypt");
        b.Scheme.Version.ShouldBe(5);
    }

    [Fact]
    public void MakesUniqueSlugsInAnyScript()
    {
        var b = new SchemeBuilder();

        b.Add("Middle East").Slug.ShouldBe("middle-east");
        b.Add("Middle-East!").Slug.ShouldBe("middle-east-2");
        b.Add("الشرق الأوسط").Slug.ShouldBe("الشرق-الاوسط");
        Should.Throw<DomainException>(() => b.Add("Other", slug: "middle-east")).Message.ShouldContain("taken");
    }

    [Fact]
    public void RejectsAParentFromNowhere() =>
        Should.Throw<DomainException>(() => new SchemeBuilder().Scheme.AddTerm(new TermValues("X"), 99, Now));

    [Fact]
    public void LimitsTheDepth()
    {
        var b = new SchemeBuilder();
        var parent = b.Add("Level 1");
        for (var i = 2; i <= TermRules.MaxDepth; i++) parent = b.Add($"Level {i}", parent);

        Should.Throw<DomainException>(() => b.Add("Too deep", parent)).Message.ShouldContain("levels deep");
    }

    [Fact]
    public void MovesATermWithItsChildrenAndRenumbersBothSides()
    {
        var b = new SchemeBuilder();
        var world = b.Add("World");
        var europe = b.Add("Europe", world);
        b.Add("France", europe);
        b.Add("Asia", world);
        b.Add("Sports");

        b.Scheme.MoveTerm(europe.Id, null, 1, Now);

        b.Tree().ShouldBe("World | -Asia | Europe | -France | Sports");
        b.Scheme.InTreeOrder().Where(t => t.ParentId == null).Select(t => t.Position).ShouldBe([0, 1, 2]);
        b.Scheme.Find(4).Position.ShouldBe(0); // Asia, alone under World now
    }

    [Fact]
    public void ReordersSiblings()
    {
        var b = new SchemeBuilder();
        b.Add("A");
        b.Add("B");
        var c = b.Add("C");

        b.Scheme.MoveTerm(c.Id, null, 0, Now);

        b.Tree().ShouldBe("C | A | B");
    }

    [Fact]
    public void NeverMovesATermUnderItself()
    {
        var b = new SchemeBuilder();
        var world = b.Add("World");
        var europe = b.Add("Europe", world);

        Should.Throw<DomainException>(() => b.Scheme.MoveTerm(world.Id, world.Id, 0, Now));
        Should.Throw<DomainException>(() => b.Scheme.MoveTerm(world.Id, europe.Id, 0, Now));
    }

    [Fact]
    public void RemovesOnlyTermsWithoutChildren()
    {
        var b = new SchemeBuilder();
        var world = b.Add("World");
        b.Add("Europe", world);
        var asia = b.Add("Asia", world);
        b.Add("Africa", world);

        Should.Throw<DomainException>(() => b.Scheme.RemoveTerm(world.Id, Now)).Message.ShouldContain("narrower terms");
        b.Scheme.RemoveTerm(asia.Id, Now);

        b.Tree().ShouldBe("World | -Europe | -Africa");
        b.Scheme.Terms.Where(t => t.ParentId == world.Id).OrderBy(t => t.Position).Select(t => t.Position).ShouldBe([0, 1]);
    }

    [Fact]
    public void EditKeepsTheSlugWhenOnlyTheCaseChanges()
    {
        var b = new SchemeBuilder();
        var term = b.Add("Middle East");

        b.Scheme.EditTerm(term.Id, new TermValues("MIDDLE EAST"), Now).Slug.ShouldBe("middle-east");
        b.Scheme.EditTerm(term.Id, new TermValues("Near East"), Now).Slug.ShouldBe("near-east");
    }

    [Fact]
    public void LinksAConceptWithItsName()
    {
        var b = new SchemeBuilder();
        var term = b.Add("Egypt");

        b.Scheme.EditTerm(term.Id, new TermValues("Egypt", "egypt", null, null, 12, "Egypt (country)"), Now);
        term.ConceptId.ShouldBe(12);
        term.ConceptName.ShouldBe("Egypt (country)");

        b.Scheme.EditTerm(term.Id, new TermValues("Egypt", "egypt", null, null, null, "dropped without an id"), Now);
        term.ConceptName.ShouldBeNull();
    }

    [Fact]
    public void JsonLdIsADefinedTermSetOfDefinedTerms()
    {
        var b = new SchemeBuilder();
        var world = b.Add("World");
        b.Scheme.EditTerm(world.Id, new TermValues("World", "world", "IPTC:16000000", "Around the world."), Now);
        b.Add("Egypt", world);

        var json = SchemeJsonLd.For(b.Scheme);
        json["@type"].ShouldBe("DefinedTermSet");
        json["inLanguage"].ShouldBe("en");
        json.ShouldNotContainKey("description");
        var terms = (List<Dictionary<string, object?>>)json["hasDefinedTerm"]!;
        terms.Select(t => t["termCode"]).ShouldBe(["IPTC:16000000", "egypt"]);
        terms[0].ShouldNotContainKey("inDefinedTermSet");

        var one = SchemeJsonLd.Term(b.Scheme, world, withContext: true);
        ((Dictionary<string, object?>)one["inDefinedTermSet"]!)["identifier"].ShouldBe("sections");
        one["description"].ShouldBe("Around the world.");
    }
}
