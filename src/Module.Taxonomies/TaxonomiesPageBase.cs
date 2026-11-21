using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Localization;
using soft.square.net.ModularBlazor.Abstractions;
using soft.square.net.ModularBlazor.Abstractions.MudTail;
using soft.square.net.WebApi.Modules.Taxonomies.Client;

namespace soft.square.net.Module.Taxonomies;

/// <summary>
/// What every page of this module shares: the API client on the host's "WebApi" HttpClient (the gateway, with the
/// access token), one-call-at-a-time state and readable error messages.
/// </summary>
public abstract class TaxonomiesPageBase : ComponentBase
{
    [Inject] protected IHttpClientFactory HttpClients { get; set; } = default!;
    [Inject] protected MudTailRuntime MudTail { get; set; } = default!;
    [Inject] protected NavigationManager Nav { get; set; } = default!;
    [Inject] protected IStringLocalizerFactory Texts { get; set; } = default!;

    private IStringLocalizer? _l;

    /// <summary>This module's texts (its embedded i18n/{culture}.json); the keys are the English texts.</summary>
    protected IStringLocalizer L => _l ??= Texts.Create(GetType());

    /// <summary>The module's routes (/api/taxonomies/...) reach the pool that runs the module through the gateway.</summary>
    protected TaxonomiesClient Api => new(HttpClients.CreateClient(HttpClientNames.WebApi));

    protected bool Busy { get; private set; }
    protected string? Error { get; set; }

    /// <summary>Runs one API call: busy flag, error message and toast, and <paramref name="then"/> when it succeeded.</summary>
    protected async Task<bool> RunAsync(Func<Task> call, Func<Task>? then = null, bool toast = true)
    {
        Busy = true;
        Error = null;
        StateHasChanged();
        try
        {
            await call();
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect(); // the session ended: sign in again and come back
            return false;
        }
        catch (Exception ex)
        {
            Error = Describe(ex);
            if (toast) await MudTail.ToastAsync(Error, "error");
            return false;
        }
        finally
        {
            Busy = false;
            StateHasChanged();
        }

        if (then is not null) await then();
        StateHasChanged();
        return true;
    }

    protected string Describe(Exception ex) => ex switch
    {
        TaxonomiesApiException { StatusCode: 400 } api when Mentions(api, "tenant") =>
            L["400: no tenant. Schemes belong to a tenant: sign in to one (your organisation) to use them."],
        TaxonomiesApiException { StatusCode: 401 } => L["401: the gateway did not accept the access token. Sign in again."],
        TaxonomiesApiException { StatusCode: 403 } api =>
            $"403: {ProblemDetail(api) ?? L["your account lacks the permission for this (taxonomies.* in this tenant)."]}",
        TaxonomiesApiException { StatusCode: 404 } api => $"404: {ProblemDetail(api) ?? L["not found."]}",
        TaxonomiesApiException { StatusCode: 409 } api => $"409: {ProblemDetail(api) ?? L["someone changed it meanwhile. Reload and try again."]}",
        TaxonomiesApiException api => $"{api.StatusCode}: {ProblemDetail(api) ?? FirstLine(api.Response) ?? FirstLine(api.Message)}",
        HttpRequestException http => L["The gateway is not reachable ({0}).", http.Message],
        _ => ex.Message,
    };

    /// <summary>The "detail" (else "title") of a problem-details response body, if it is one.</summary>
    private static string? ProblemDetail(TaxonomiesApiException api)
    {
        if (string.IsNullOrWhiteSpace(api.Response) || !api.Response.TrimStart().StartsWith('{')) return null;
        try
        {
            using var json = System.Text.Json.JsonDocument.Parse(api.Response);
            foreach (var name in new[] { "detail", "title" })
                if (json.RootElement.TryGetProperty(name, out var value) && value.GetString() is { Length: > 0 } text)
                    return text;
        }
        catch (System.Text.Json.JsonException) { }
        return null;
    }

    private static bool Mentions(TaxonomiesApiException api, string word) =>
        (api.Response ?? "").Contains(word, StringComparison.OrdinalIgnoreCase);

    private static string? FirstLine(string? text)
        => string.IsNullOrWhiteSpace(text) ? null : text.Split('\n', 2)[0].Trim();

    protected static string When(DateTimeOffset? at) => at?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "";
}
