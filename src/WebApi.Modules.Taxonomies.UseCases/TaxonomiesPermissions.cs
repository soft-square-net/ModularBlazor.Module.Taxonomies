namespace soft.square.net.WebApi.Modules.Taxonomies.UseCases;

/// <summary>
/// The module's permissions, named <c>&lt;module&gt;.&lt;action&gt;</c>. Declare every permission the module uses (the
/// manifest lists <see cref="All"/>); activation fails on an undeclared one. Admins grant them per tenant.
/// </summary>
public static class TaxonomiesPermissions
{
    public const string Read = "taxonomies.read";
    public const string Manage = "taxonomies.manage";

    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        new(Read, "Read schemes and their terms, search terms, and their JSON-LD."),
        new(Manage, "Create, change and delete schemes and their terms.")
    ];
}
