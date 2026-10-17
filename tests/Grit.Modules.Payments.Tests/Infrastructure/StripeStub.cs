using System.Net;
using System.Text;
using Stripe;

namespace Grit.Modules.Payments.Tests.Infrastructure;

// Stands in for api.stripe.com: answers every request with one recorded response and keeps the last request.
internal sealed class StripeStub(HttpStatusCode status, string responseJson) : HttpMessageHandler
{
    public HttpRequestMessage? Request { get; private set; }

    public string RequestForm { get; private set; } = string.Empty;

    public StripeClient Client() =>
        new("sk_test_stub", httpClient: new SystemNetHttpClient(new HttpClient(this), maxNetworkRetries: 0));

    public string? Header(string name) =>
        Request is not null && Request.Headers.TryGetValues(name, out var values) ? values.Single() : null;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Request = request;
        RequestForm = request.Content is null ? string.Empty : WebUtility.UrlDecode(await request.Content.ReadAsStringAsync(cancellationToken));
        return new HttpResponseMessage(status) { Content = new StringContent(responseJson, Encoding.UTF8, "application/json") };
    }
}
