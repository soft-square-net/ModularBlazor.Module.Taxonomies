using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using soft.square.net.WebApi.Modules.Taxonomies.Core.SchemeAggregate;

namespace soft.square.net.WebApi.Modules.Taxonomies.Infrastructure.Data.Config;

public sealed class SchemeConfiguration : IEntityTypeConfiguration<TaxonomyScheme>
{
    public void Configure(EntityTypeBuilder<TaxonomyScheme> scheme)
    {
        scheme.ToTable("schemes");
        scheme.Property(s => s.Key).HasMaxLength(TermRules.KeyMaxLength);
        // A key is unique per tenant (the platform's TenantDbContext maps TenantId: 64 characters, indexed).
        scheme.HasIndex(nameof(TaxonomyScheme.TenantId), nameof(TaxonomyScheme.Key)).IsUnique();
        scheme.Property(s => s.Name).HasMaxLength(TermRules.NameMaxLength);
        scheme.Property(s => s.Description).HasMaxLength(TermRules.DescriptionMaxLength);
        scheme.Property(s => s.Language).HasMaxLength(TermRules.LanguageMaxLength);
        scheme.Property(s => s.CreatedBy).HasMaxLength(DataSchemaConstants.UserIdLength);
        scheme.Property(s => s.Version).IsConcurrencyToken();

        scheme.HasMany(s => s.Terms).WithOne().HasForeignKey("SchemeId").IsRequired().OnDelete(DeleteBehavior.Cascade);
        scheme.Navigation(s => s.Terms).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class TermConfiguration : IEntityTypeConfiguration<Term>
{
    public void Configure(EntityTypeBuilder<Term> term)
    {
        term.ToTable("terms");
        // The tree is checked by the scheme (same scheme, no cycles); ParentId is a plain column, so removing and
        // moving terms never trips a self-referencing constraint on any engine.
        term.HasIndex(t => t.ParentId);
        term.HasIndex("SchemeId", nameof(Term.Slug)).IsUnique();
        term.Property(t => t.Name).HasMaxLength(TermRules.NameMaxLength);
        term.Property(t => t.Slug).HasMaxLength(TermRules.SlugMaxLength);
        term.Property(t => t.Code).HasMaxLength(TermRules.CodeMaxLength);
        term.Property(t => t.Description).HasMaxLength(TermRules.DescriptionMaxLength);
        term.Property(t => t.ConceptName).HasMaxLength(TermRules.NameMaxLength);
        term.HasIndex(t => t.ConceptId);
    }
}

/// <summary>Column sizes shared by the mappings.</summary>
public static class DataSchemaConstants
{
    /// <summary>A user id (the token's <c>sub</c>).</summary>
    public const int UserIdLength = 128;
}
