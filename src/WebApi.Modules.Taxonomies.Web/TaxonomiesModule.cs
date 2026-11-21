using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using soft.square.net.WebApi.Contracts;
using soft.square.net.WebApi.Modules.Taxonomies.Web.Configurations;

namespace soft.square.net.WebApi.Modules.Taxonomies.Web;

/// <summary>
/// The module: its manifest (id, name, version, route prefix, default settings, permissions) and its services.
/// This is the composition root (the Ardalis template's Program.cs): the runtime creates it, reads the manifest and
/// calls <see cref="ConfigureServices"/> on the module's own container; the endpoints are in
/// <see cref="TaxonomiesRoutes"/>, mapped under <c>/api/taxonomies</c>. Tenant data, schema "taxonomies".
/// </summary>
public sealed class TaxonomiesModule : IRuntimeModule
{
    public ModuleManifest Manifest { get; } = new("taxonomies", "Taxonomies", "0.1.0", "taxonomies")
    {
        Description = "Taxonomies: classification schemes (sections, genres, categories) as trees of terms, each optionally standing for a Keywords concept, as schema.org DefinedTermSet JSON-LD.",
        DefaultSettings = new Dictionary<string, string?> { ["Taxonomies:MaxPageSize"] = "100" },
        Permissions = TaxonomiesPermissions.All
    };

    public void ConfigureServices(IServiceCollection services, IConfiguration settings) => services.AddServiceConfigs(settings);
}
