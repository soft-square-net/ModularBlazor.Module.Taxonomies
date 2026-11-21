using Carter;
using Schemes = soft.square.net.WebApi.Modules.Taxonomies.Web.Schemes;

namespace soft.square.net.WebApi.Modules.Taxonomies.Web;

/// <summary>
/// The module's route table, and its only Carter module (the runtime maps the Carter modules of the entry assembly,
/// under <c>/api/taxonomies</c>). Every endpoint is listed here once, in the order of the OpenAPI document. A new
/// endpoint: add its file under the resource folder, then one line here.
/// </summary>
public sealed class TaxonomiesRoutes : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        // Problem details for domain rules and lost races, on the module's own groups (module code, unloads with it).
        var schemes = app.MapGroup("/schemes").WithTags("Schemes").AddEndpointFilter<TaxonomiesProblemsFilter>();
        var terms = app.MapGroup("/terms").WithTags("Terms").AddEndpointFilter<TaxonomiesProblemsFilter>();

        Schemes.List.Map(schemes);
        Schemes.Create.Map(schemes);
        Schemes.Get.Map(schemes);
        Schemes.JsonLd.Map(schemes);
        Schemes.Edit.Map(schemes);
        Schemes.Delete.Map(schemes);
        Schemes.AddTerm.Map(schemes);
        Schemes.EditTerm.Map(schemes);
        Schemes.MoveTerm.Map(schemes);
        Schemes.RemoveTerm.Map(schemes);

        Schemes.SearchTerms.Map(terms);
        Schemes.LookupTerms.Map(terms);
        Schemes.TermJsonLd.Map(terms);
    }
}
