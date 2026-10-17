using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Grit.MlImporter;

internal sealed class MercadoLivreClient(HttpClient http, string accessToken)
{
    public static readonly Uri ApiAddress = new("https://api.mercadolibre.com/");

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public async Task<IReadOnlyList<MlProduct>> SearchAsync(string query, int limit, CancellationToken cancellationToken)
    {
        var path = $"products/search?status=active&site_id=MLB&q={Uri.EscapeDataString(query)}&limit={limit}";
        using var response = await SendAsync(path, cancellationToken);
        return (await ReadAsync<MlPage<MlProduct>>(response, cancellationToken)).Results;
    }

    public async Task<MlProduct> ProductAsync(string id, CancellationToken cancellationToken)
    {
        using var response = await SendAsync($"products/{Uri.EscapeDataString(id)}", cancellationToken);
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

    // Pictures are public CDN files: the token goes only to the API host.
    public Task<byte[]> DownloadAsync(string url, CancellationToken cancellationToken) => http.GetByteArrayAsync(url, cancellationToken);

    private async Task<HttpResponseMessage> SendAsync(string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(ApiAddress, path));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await http.SendAsync(request, cancellationToken);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            ?? throw new HttpRequestException($"Mercado Livre answered an empty body for {response.RequestMessage?.RequestUri}.");
    }
}
