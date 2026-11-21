using soft.square.net.WebApi.Modules.Taxonomies.UseCases.Notes;
using soft.square.net.WebApi.Modules.Taxonomies.UseCases.Notes.Edit;

namespace soft.square.net.WebApi.Modules.Taxonomies.Web.Notes;

/// <summary>PUT /notes/{id}: change a note; the body carries the version the caller read (409 if it changed since).</summary>
public sealed class Edit : IEndpoint
{
    /// <summary>The request body.</summary>
    public sealed record NoteEdit(int Version, string Title, string? Body);

    public static RouteHandlerBuilder Map(IEndpointRouteBuilder notes) =>
        notes.MapPut("/{id:int}", HandleAsync)
            .RequirePermission(TaxonomiesPermissions.Write)
            .WithName("Taxonomies_EditNote");

    private static async Task<Results<Ok<NoteDto>, ProblemHttpResult>> HandleAsync(int id, NoteEdit edit, ISender sender, CancellationToken ct) =>
        (await sender.Send(new EditNoteCommand(id, edit.Version, edit.Title, edit.Body), ct)).ToOkResult();
}
