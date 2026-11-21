using soft.square.net.WebApi.Modules.Taxonomies.Core.SchemeAggregate;
using soft.square.net.WebApi.Modules.Taxonomies.Core.SchemeAggregate.Specifications;

namespace soft.square.net.WebApi.Modules.Taxonomies.UseCases.Schemes;

/// <summary>Create an empty scheme; the caller becomes its author. Its key cannot change later.</summary>
public sealed record CreateSchemeCommand(string Key, SchemeValues Values) : ICommand<Result<SchemeDto>>;

public sealed class CreateSchemeHandler(IRepository<TaxonomyScheme> schemes, IAccessScopes access, TimeProvider clock)
    : ICommandHandler<CreateSchemeCommand, Result<SchemeDto>>
{
    public async ValueTask<Result<SchemeDto>> Handle(CreateSchemeCommand command, CancellationToken ct)
    {
        if (access.UserId is not { Length: > 0 } me) return Result<SchemeDto>.Unauthorized("Sign in first.");

        var scheme = TaxonomyScheme.Create(command.Key, command.Values, me, clock.GetUtcNow());
        if (await schemes.AnyAsync(new SchemeReadSpec(scheme.Key), ct)) return Result<SchemeDto>.Conflict($"Scheme '{scheme.Key}' exists already.");

        await schemes.AddAsync(scheme, ct);
        return SchemeDto.From(scheme);
    }
}

/// <summary>Delete a scheme that has no terms left. <paramref name="Version"/> is the one the caller read.</summary>
public sealed record DeleteSchemeCommand(string Key, int Version) : ICommand<Result>;

public sealed class DeleteSchemeHandler(IRepository<TaxonomyScheme> schemes) : ICommandHandler<DeleteSchemeCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteSchemeCommand command, CancellationToken ct)
    {
        var scheme = await schemes.FirstOrDefaultAsync(new SchemeByKeySpec(command.Key), ct);
        if (scheme is null) return Result.NotFound(SchemeResults.NotFoundMessage(command.Key));
        if (scheme.Version != command.Version) return Result.Conflict(SchemeResults.StaleMessage(command.Key, scheme.Version, command.Version));
        if (scheme.Terms.Count > 0) return Result.Conflict($"Scheme '{command.Key}' still has {scheme.Terms.Count} terms; remove them first.");

        await schemes.DeleteAsync(scheme, ct);
        return Result.Success();
    }
}

/// <summary>A change to a scheme or one of its terms, at the scheme <paramref name="Version"/> the caller read.</summary>
public abstract record SchemeChange(string Key, int Version) : ICommand<Result<SchemeDto>>
{
    /// <summary>Applies the change through the aggregate (its rules throw <see cref="DomainException"/>).</summary>
    public abstract void Apply(TaxonomyScheme scheme, DateTimeOffset now);
}

/// <summary>Rename a scheme, or change its description or language.</summary>
public sealed record EditSchemeCommand(string Key, int Version, SchemeValues Values) : SchemeChange(Key, Version)
{
    public override void Apply(TaxonomyScheme scheme, DateTimeOffset now) => scheme.Edit(Values, now);
}

/// <summary>Add a term under <paramref name="ParentId"/> (null: at the top), after its siblings.</summary>
public sealed record AddTermCommand(string Key, int Version, int? ParentId, TermValues Values) : SchemeChange(Key, Version)
{
    public override void Apply(TaxonomyScheme scheme, DateTimeOffset now) => scheme.AddTerm(Values, ParentId, now);
}

/// <summary>Change a term's name, slug, code, description or concept.</summary>
public sealed record EditTermCommand(string Key, int Version, int TermId, TermValues Values) : SchemeChange(Key, Version)
{
    public override void Apply(TaxonomyScheme scheme, DateTimeOffset now) => scheme.EditTerm(TermId, Values, now);
}

/// <summary>Move a term (and its narrower terms) under <paramref name="ParentId"/> (null: the top) at <paramref name="Position"/>.</summary>
public sealed record MoveTermCommand(string Key, int Version, int TermId, int? ParentId, int Position) : SchemeChange(Key, Version)
{
    public override void Apply(TaxonomyScheme scheme, DateTimeOffset now) => scheme.MoveTerm(TermId, ParentId, Position, now);
}

/// <summary>Remove a term with no narrower terms.</summary>
public sealed record RemoveTermCommand(string Key, int Version, int TermId) : SchemeChange(Key, Version)
{
    public override void Apply(TaxonomyScheme scheme, DateTimeOffset now) => scheme.RemoveTerm(TermId, now);
}

/// <summary>
/// Load, check the caller's version, change through the aggregate, save, answer with the scheme as it is now. A change
/// that races this one between the check and the save fails on the version's concurrency token (409).
/// </summary>
public sealed class SchemeChangeHandler(IRepository<TaxonomyScheme> schemes, TimeProvider clock) :
    ICommandHandler<EditSchemeCommand, Result<SchemeDto>>,
    ICommandHandler<AddTermCommand, Result<SchemeDto>>,
    ICommandHandler<EditTermCommand, Result<SchemeDto>>,
    ICommandHandler<MoveTermCommand, Result<SchemeDto>>,
    ICommandHandler<RemoveTermCommand, Result<SchemeDto>>
{
    public ValueTask<Result<SchemeDto>> Handle(EditSchemeCommand command, CancellationToken ct) => ChangeAsync(command, ct);
    public ValueTask<Result<SchemeDto>> Handle(AddTermCommand command, CancellationToken ct) => ChangeAsync(command, ct);
    public ValueTask<Result<SchemeDto>> Handle(EditTermCommand command, CancellationToken ct) => ChangeAsync(command, ct);
    public ValueTask<Result<SchemeDto>> Handle(MoveTermCommand command, CancellationToken ct) => ChangeAsync(command, ct);
    public ValueTask<Result<SchemeDto>> Handle(RemoveTermCommand command, CancellationToken ct) => ChangeAsync(command, ct);

    private async ValueTask<Result<SchemeDto>> ChangeAsync(SchemeChange change, CancellationToken ct)
    {
        var scheme = await schemes.FirstOrDefaultAsync(new SchemeByKeySpec(change.Key), ct);
        if (scheme is null) return Result<SchemeDto>.NotFound(SchemeResults.NotFoundMessage(change.Key));
        if (scheme.Version != change.Version) return Result<SchemeDto>.Conflict(SchemeResults.StaleMessage(change.Key, scheme.Version, change.Version));

        change.Apply(scheme, clock.GetUtcNow());
        await schemes.UpdateAsync(scheme, ct);
        return SchemeDto.From(scheme);
    }
}
