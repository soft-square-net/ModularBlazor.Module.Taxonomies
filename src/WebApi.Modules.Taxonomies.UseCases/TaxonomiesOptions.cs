namespace soft.square.net.WebApi.Modules.Taxonomies.UseCases;

/// <summary>Module settings (section <c>Taxonomies</c>; defaults in the manifest, changed on the Host Modules page).</summary>
public sealed class TaxonomiesOptions
{
    /// <summary>The largest page a list or search returns.</summary>
    public int MaxPageSize { get; set; } = 100;
}
