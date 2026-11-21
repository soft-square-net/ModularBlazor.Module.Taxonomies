using Microsoft.AspNetCore.Mvc;
using soft.square.net.WebApi.Modules.Taxonomies.Core.SchemeAggregate;
using soft.square.net.WebApi.Modules.Taxonomies.UseCases.Schemes;

namespace soft.square.net.WebApi.Modules.Taxonomies.Web.Schemes;

/// <summary>GET /schemes: the tenant's schemes, by name, with their term counts.</summary>
public sealed class List : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder schemes) =>
        schemes.MapGet("/", HandleAsync)
            .RequirePermission(TaxonomiesPermissions.Read)
            .WithName("Taxonomies_ListSchemes");

    private static async Task<Results<Ok<IReadOnlyList<SchemeSummaryDto>>, ProblemHttpResult>> HandleAsync(ISender sender, CancellationToken ct) =>
        (await sender.Send(new ListSchemesQuery(), ct)).ToOkResult();
}

/// <summary>POST /schemes: create an empty scheme; 201 with it.</summary>
public sealed class Create : IEndpoint
{
    /// <summary>The request body.</summary>
    public sealed record NewScheme(string Key, SchemeValues Values);

    public static RouteHandlerBuilder Map(IEndpointRouteBuilder schemes) =>
        schemes.MapPost("/", HandleAsync)
            .RequirePermission(TaxonomiesPermissions.Manage)
            .WithName("Taxonomies_CreateScheme");

    private static async Task<Results<Created<SchemeDto>, ProblemHttpResult>> HandleAsync(NewScheme request, ISender sender, CancellationToken ct) =>
        (await sender.Send(new CreateSchemeCommand(request.Key, request.Values), ct)).ToCreatedResult(scheme => $"schemes/{scheme.Key}");
}

/// <summary>GET /schemes/{key}: a scheme with all its terms, in tree order.</summary>
public sealed class Get : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder schemes) =>
        schemes.MapGet("/{key}", HandleAsync)
            .RequirePermission(TaxonomiesPermissions.Read)
            .WithName("Taxonomies_GetScheme");

    private static async Task<Results<Ok<SchemeDto>, ProblemHttpResult>> HandleAsync(string key, ISender sender, CancellationToken ct) =>
        (await sender.Send(new GetSchemeQuery(key), ct)).ToOkResult();
}

/// <summary>GET /schemes/{key}/jsonld: the scheme as a schema.org DefinedTermSet with its terms.</summary>
public sealed class JsonLd : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder schemes) =>
        schemes.MapGet("/{key}/jsonld", HandleAsync)
            .RequirePermission(TaxonomiesPermissions.Read)
            .WithName("Taxonomies_GetSchemeJsonLd");

    private static async Task<Results<Ok<JsonLdDto>, ProblemHttpResult>> HandleAsync(string key, ISender sender, CancellationToken ct) =>
        (await sender.Send(new GetSchemeJsonLdQuery(key), ct)).ToOkResult();
}

/// <summary>PUT /schemes/{key}: rename a scheme, or change its description or language.</summary>
public sealed class Edit : IEndpoint
{
    /// <summary>The request body.</summary>
    public sealed record SchemeEdit(int Version, SchemeValues Values);

    public static RouteHandlerBuilder Map(IEndpointRouteBuilder schemes) =>
        schemes.MapPut("/{key}", HandleAsync)
            .RequirePermission(TaxonomiesPermissions.Manage)
            .WithName("Taxonomies_EditScheme");

    private static async Task<Results<Ok<SchemeDto>, ProblemHttpResult>> HandleAsync(string key, SchemeEdit edit, ISender sender, CancellationToken ct) =>
        (await sender.Send(new EditSchemeCommand(key, edit.Version, edit.Values), ct)).ToOkResult();
}

/// <summary>DELETE /schemes/{key}?version=: delete a scheme that has no terms left.</summary>
public sealed class Delete : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder schemes) =>
        schemes.MapDelete("/{key}", HandleAsync)
            .RequirePermission(TaxonomiesPermissions.Manage)
            .WithName("Taxonomies_DeleteScheme");

    private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(string key, int version, ISender sender, CancellationToken ct) =>
        (await sender.Send(new DeleteSchemeCommand(key, version), ct)).ToNoContentResult();
}

/// <summary>POST /schemes/{key}/terms: add a term under parentId (none: at the top); answers with the scheme.</summary>
public sealed class AddTerm : IEndpoint
{
    /// <summary>The request body.</summary>
    public sealed record NewTerm(int Version, int? ParentId, TermValues Values);

    public static RouteHandlerBuilder Map(IEndpointRouteBuilder schemes) =>
        schemes.MapPost("/{key}/terms", HandleAsync)
            .RequirePermission(TaxonomiesPermissions.Manage)
            .WithName("Taxonomies_AddTerm");

    private static async Task<Results<Ok<SchemeDto>, ProblemHttpResult>> HandleAsync(string key, NewTerm request, ISender sender, CancellationToken ct) =>
        (await sender.Send(new AddTermCommand(key, request.Version, request.ParentId, request.Values), ct)).ToOkResult();
}

/// <summary>PUT /schemes/{key}/terms/{termId}: change a term; answers with the scheme.</summary>
public sealed class EditTerm : IEndpoint
{
    /// <summary>The request body.</summary>
    public sealed record TermEdit(int Version, TermValues Values);

    public static RouteHandlerBuilder Map(IEndpointRouteBuilder schemes) =>
        schemes.MapPut("/{key}/terms/{termId:int}", HandleAsync)
            .RequirePermission(TaxonomiesPermissions.Manage)
            .WithName("Taxonomies_EditTerm");

    private static async Task<Results<Ok<SchemeDto>, ProblemHttpResult>> HandleAsync(string key, int termId, TermEdit edit, ISender sender, CancellationToken ct) =>
        (await sender.Send(new EditTermCommand(key, edit.Version, termId, edit.Values), ct)).ToOkResult();
}

/// <summary>POST /schemes/{key}/terms/{termId}/move: move a term (and what is under it); answers with the scheme.</summary>
public sealed class MoveTerm : IEndpoint
{
    /// <summary>The request body: the new parent (none: the top) and the position among its children, from 0.</summary>
    public sealed record TermMove(int Version, int? ParentId, int Position);

    public static RouteHandlerBuilder Map(IEndpointRouteBuilder schemes) =>
        schemes.MapPost("/{key}/terms/{termId:int}/move", HandleAsync)
            .RequirePermission(TaxonomiesPermissions.Manage)
            .WithName("Taxonomies_MoveTerm");

    private static async Task<Results<Ok<SchemeDto>, ProblemHttpResult>> HandleAsync(string key, int termId, TermMove move, ISender sender, CancellationToken ct) =>
        (await sender.Send(new MoveTermCommand(key, move.Version, termId, move.ParentId, move.Position), ct)).ToOkResult();
}

/// <summary>DELETE /schemes/{key}/terms/{termId}?version=: remove a term with no narrower terms; answers with the scheme.</summary>
public sealed class RemoveTerm : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder schemes) =>
        schemes.MapDelete("/{key}/terms/{termId:int}", HandleAsync)
            .RequirePermission(TaxonomiesPermissions.Manage)
            .WithName("Taxonomies_RemoveTerm");

    private static async Task<Results<Ok<SchemeDto>, ProblemHttpResult>> HandleAsync(string key, int termId, int version, ISender sender, CancellationToken ct) =>
        (await sender.Send(new RemoveTermCommand(key, version, termId), ct)).ToOkResult();
}

/// <summary>GET /terms?search=&amp;scheme=&amp;limit=: terms matching the text, in tree order (the picker's search).</summary>
public sealed class SearchTerms : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder terms) =>
        terms.MapGet("/", HandleAsync)
            .RequirePermission(TaxonomiesPermissions.Read)
            .WithName("Taxonomies_SearchTerms")
            .WithSummary("Terms whose name, slug, code or concept contains the text (case, accents and Arabic letter forms ignored), in tree order.");

    private static async Task<Results<Ok<IReadOnlyList<TermRefDto>>, ProblemHttpResult>> HandleAsync(ISender sender, CancellationToken ct,
        string? search = null, string? scheme = null, int limit = 50) =>
        (await sender.Send(new SearchTermsQuery(search, scheme, limit), ct)).ToOkResult();
}

/// <summary>GET /terms/lookup?ids=1&amp;ids=2: the terms with these ids, in that order.</summary>
public sealed class LookupTerms : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder terms) =>
        terms.MapGet("/lookup", HandleAsync)
            .RequirePermission(TaxonomiesPermissions.Read)
            .WithName("Taxonomies_LookupTerms")
            .WithSummary("The terms with these ids, in the order asked, with their scheme and path; removed ones are left out.");

    private static async Task<Results<Ok<IReadOnlyList<TermRefDto>>, ProblemHttpResult>> HandleAsync([FromQuery] int[] ids, ISender sender, CancellationToken ct) =>
        (await sender.Send(new LookupTermsQuery(ids), ct)).ToOkResult();
}

/// <summary>GET /terms/{id}/jsonld: a term as a schema.org DefinedTerm in its DefinedTermSet.</summary>
public sealed class TermJsonLd : IEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder terms) =>
        terms.MapGet("/{id:int}/jsonld", HandleAsync)
            .RequirePermission(TaxonomiesPermissions.Read)
            .WithName("Taxonomies_GetTermJsonLd");

    private static async Task<Results<Ok<JsonLdDto>, ProblemHttpResult>> HandleAsync(int id, ISender sender, CancellationToken ct) =>
        (await sender.Send(new GetTermJsonLdQuery(id), ct)).ToOkResult();
}
