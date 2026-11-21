namespace soft.square.net.WebApi.Modules.Taxonomies.Web;

/// <summary>
/// One HTTP endpoint (REPR: its request type, the endpoint, its response), mapped by <see cref="TaxonomiesRoutes"/>.
/// The endpoint sets its route, permission, name (the OpenAPI operation id, <c>Taxonomies_&lt;Operation&gt;</c>, which
/// names the generated client's method) and summary, and sends one command or query. No logic beyond binding.
/// </summary>
public interface IEndpoint
{
    static abstract RouteHandlerBuilder Map(IEndpointRouteBuilder app);
}
