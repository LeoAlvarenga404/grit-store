using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.Modules.Catalog.Infrastructure;

// Public catalog reads only; stock never goes here (design §9). Caching:Enabled=false bypasses it entirely, reads and
// invalidations, so k6 can compare runs with and without it.
internal sealed class CatalogCache(HybridCache cache, IConfiguration configuration)
{
    // ponytail: one tag for the whole catalog, since almost every admin write also changes the listing, facets or
    // suggestions; per-product tags if writes ever get frequent.
    public const string Tag = "catalog";

    private static readonly string[] Tags = [Tag];

    // L1 expires sooner, bounding how stale another API instance can be: HybridCache has no backplane.
    private static readonly HybridCacheEntryOptions Entry = new()
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(1),
    };

    private readonly bool _isEnabled = configuration.GetValue("Caching:Enabled", true);

    public static string KeyOf<TQuery>(string kind, TQuery query) => $"catalog:{kind}:{JsonSerializer.Serialize(query)}";

    // The suggestion SQL unaccents the text, and pg_trgm and starts_with(lower(...)) ignore case, so these variants
    // always get the same answer and can share one entry.
    public static string SuggestionsKeyOf(string text)
    {
        var decomposed = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var unaccented = string.Concat(decomposed.Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark));
        return $"catalog:suggestions:{unaccented}";
    }

    public static bool IsSuccess(object? result) =>
        (result is INestedHttpResult nested ? nested.Result : result) is IStatusCodeHttpResult { StatusCode: >= 200 and < 300 };

    // Runs around the endpoint, so the command's transaction (TransactionBehavior, inside sender.Send) has committed:
    // invalidating inside the handler would let a concurrent read cache the old row again before the commit.
    public static async ValueTask<object?> InvalidateAfterWrite(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);
        if (HttpMethods.IsGet(context.HttpContext.Request.Method) || !IsSuccess(result))
        {
            return result;
        }

        await context.HttpContext.RequestServices.GetRequiredService<CatalogCache>().InvalidateAsync();
        return result;
    }

    public async ValueTask<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T>> load, CancellationToken cancellationToken) =>
        _isEnabled ? await cache.GetOrCreateAsync(key, load, Entry, Tags, cancellationToken) : await load(cancellationToken);

    // Not the request's token: a client that hangs up after the commit must not leave stale entries behind.
    public ValueTask InvalidateAsync() => _isEnabled ? cache.RemoveByTagAsync(Tag, CancellationToken.None) : ValueTask.CompletedTask;
}
