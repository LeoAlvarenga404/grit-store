using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Grit.MlImporter;

internal sealed class MercadoLivreClient(HttpClient http, string accessToken, TimeSpan retryDelay)
{
    public const int MaxAttempts = 5;

    // The widest page the search serves; offsets move by it.
    public const int SearchLimit = 50;

    public static readonly Uri ApiAddress = new("https://api.mercadolibre.com/");

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    // 429 and the gateway errors are passing states of the API: retried with a growing wait. Anything else ends the run.
    private static readonly HashSet<HttpStatusCode> RetriedStatuses =
    [
        HttpStatusCode.TooManyRequests, HttpStatusCode.InternalServerError, HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable, HttpStatusCode.GatewayTimeout,
    ];

    public async Task<MlPage<MlProduct>> SearchAsync(string query, string? domain, int offset, CancellationToken cancellationToken)
    {
        var path = $"products/search?status=active&site_id=MLB&q={Uri.EscapeDataString(query)}&limit={SearchLimit}&offset={offset}";
        if (domain is not null)
        {
            path += $"&domain_id={Uri.EscapeDataString(domain)}";
        }

        using var response = await SendAsync(path, cancellationToken);
        return await ReadAsync<MlPage<MlProduct>>(response, cancellationToken);
    }

    // Null on 404: a parent or a picker can point at a product the catalog no longer serves.
    public async Task<MlProduct?> ProductAsync(string id, CancellationToken cancellationToken)
    {
        using var response = await SendAsync($"products/{Uri.EscapeDataString(id)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        return await ReadAsync<MlProduct>(response, cancellationToken);
    }

    // 404 "No winners found": no active listing sells the catalog product, so there is no price to import.
    public async Task<IReadOnlyList<MlItem>> ItemsAsync(string id, CancellationToken cancellationToken)
    {
        using var response = await SendAsync($"products/{Uri.EscapeDataString(id)}/items", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        return (await ReadAsync<MlPage<MlItem>>(response, cancellationToken)).Results;
    }

    // Pictures are public CDN files: the token goes only to the API host. A missing or failing picture comes back empty,
    // which WebpImage rejects, so the caller skips that picture instead of losing the run.
    public async Task<byte[]> DownloadAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(url, cancellationToken);
        return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync(cancellationToken) : [];
    }

    private async Task<HttpResponseMessage> SendAsync(string path, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var response = await SendOnceAsync(path, cancellationToken);
            if (!RetriedStatuses.Contains(response.StatusCode) || attempt == MaxAttempts)
            {
                return response;
            }

            response.Dispose();
            await Task.Delay(retryDelay * attempt, cancellationToken);
        }
    }

    private async Task<HttpResponseMessage> SendOnceAsync(string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(ApiAddress, path));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await http.SendAsync(request, cancellationToken);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"{(int)response.StatusCode} ({response.StatusCode}) for {response.RequestMessage?.RequestUri?.PathAndQuery}", null, response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new HttpRequestException($"Mercado Livre answered an empty body for {response.RequestMessage?.RequestUri}.");
    }
}
