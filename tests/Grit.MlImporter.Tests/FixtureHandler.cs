using System.Net;
using System.Net.Http.Headers;

namespace Grit.MlImporter.Tests;

// Replays the hand-written Mercado Livre responses in Fixtures/; an unexpected URL fails the test instead of going out.
internal sealed class FixtureHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpResponseMessage>> _responses = [];

    private readonly Dictionary<string, int> _throttledRequests = [];

    public List<(string Url, string? Authorization)> Requests { get; } = [];

    public FixtureHandler Json(string url, string fixture, HttpStatusCode status = HttpStatusCode.OK)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fixture);
        _responses[url] = () => new HttpResponseMessage(status)
        {
            Content = new StringContent(File.ReadAllText(path), MediaTypeHeaderValue.Parse("application/json; charset=utf-8")),
        };
        return this;
    }

    public FixtureHandler Bytes(string url, byte[] bytes)
    {
        _responses[url] = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
        return this;
    }

    public FixtureHandler Throttled(string url, int times)
    {
        _throttledRequests[url] = times;
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri!.AbsoluteUri;
        Requests.Add((url, request.Headers.Authorization?.ToString()));
        if (_throttledRequests.TryGetValue(url, out var remaining) && remaining > 0)
        {
            _throttledRequests[url] = remaining - 1;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                RequestMessage = request,
                Content = new StringContent("""{"message":"local_rate_limited","status":429}""", MediaTypeHeaderValue.Parse("application/json; charset=utf-8")),
            });
        }

        if (!_responses.TryGetValue(url, out var response))
        {
            throw new InvalidOperationException($"No fixture for {url}");
        }

        var message = response();
        message.RequestMessage = request;
        return Task.FromResult(message);
    }
}
