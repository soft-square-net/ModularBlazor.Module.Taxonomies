using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using soft.square.net.WebApi.Modules.Taxonomies.Infrastructure;

namespace soft.square.net.WebApi.Modules.Taxonomies.Web.Configurations;

public static class ServiceConfigs
{
    /// <summary>
    /// Everything the module registers, in the module's own container (never the host's): settings, data access, any
    /// services the use cases share, and the mediator with every handler.
    /// </summary>
    public static IServiceCollection AddServiceConfigs(this IServiceCollection services, IConfiguration settings)
    {
        services.Configure<TaxonomiesOptions>(settings.GetSection("Taxonomies"));
        services.AddInfrastructureServices();

        // Services shared by several use cases go here (AddScoped<...>()); handlers themselves need no registration.

        services.AddMediatorSourceGen();
        return services;
    }
}
