using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Grit.IntegrationTests;

internal static class CheckoutApi
{
    public const string OrdersPath = "/api/v1/orders";

    public static Task<HttpResponseMessage> PlaceOrderAsync(HttpClient client, string? idempotencyKey, string paymentToken, decimal expectedSubtotalAmount)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, OrdersPath)
        {
            Content = JsonContent.Create(new { paymentToken, expectedSubtotalAmount }),
        };
        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        return client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    public static async Task<Guid> PlaceOrderIdAsync(HttpClient client, string paymentToken, decimal expectedSubtotalAmount)
    {
        var response = await PlaceOrderAsync(client, Guid.NewGuid().ToString(), paymentToken, expectedSubtotalAmount);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await CatalogAdminApi.ReadAsync(response)).GetProperty("orderId").GetGuid();
    }

    public static Task<JsonElement> GetOrderAsync(HttpClient client, Guid orderId) =>
        client.GetFromJsonAsync<JsonElement>($"{OrdersPath}/{orderId}", TestContext.Current.CancellationToken);

    public static Task<JsonElement> ListOrdersAsync(HttpClient client) =>
        client.GetFromJsonAsync<JsonElement>(OrdersPath, TestContext.Current.CancellationToken);

    public static async Task<string?> CodeOf(HttpResponseMessage response) =>
        (await CatalogAdminApi.ReadAsync(response)).GetProperty("code").GetString();
}
