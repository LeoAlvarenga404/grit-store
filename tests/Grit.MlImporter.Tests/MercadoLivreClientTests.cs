using System.Net;

namespace Grit.MlImporter.Tests;

// The retry policy with the waits captured instead of slept: a real run waits 5, 10, 15 and 20 s.
public sealed class MercadoLivreClientTests : IDisposable
{
    private const string PictureUrl = $"{RecordedCatalog.Picture}700001-MLA2001-F.jpg";

    private readonly List<TimeSpan> _waits = [];

    private readonly List<HttpClient> _clients = [];

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task SearchAsync_StatusThatKeepsFailing_TriesFiveTimesWaitingLonger(HttpStatusCode status)
    {
        var handler = new FixtureHandler().Failing(RecordedCatalog.FirstPage, 10, status);

        await Should.ThrowAsync<HttpRequestException>(() => Client(handler).SearchAsync("camiseta dry fit", "MLB-T_SHIRTS", 0, TestContext.Current.CancellationToken));

        handler.Requests.Count.ShouldBe(5);
        _waits.ShouldBe([TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20)]);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task SearchAsync_ClientError_IsNotRetried(HttpStatusCode status)
    {
        var handler = new FixtureHandler().Failing(RecordedCatalog.FirstPage, 10, status);

        await Should.ThrowAsync<HttpRequestException>(() => Client(handler).SearchAsync("camiseta dry fit", "MLB-T_SHIRTS", 0, TestContext.Current.CancellationToken));

        handler.Requests.Count.ShouldBe(1);
        _waits.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task DownloadAsync_FailingStatusWithAnImageBody_ReturnsNothing(HttpStatusCode status)
    {
        var handler = new FixtureHandler().Bytes(PictureUrl, TestImages.Jpeg(10, 10), status);

        var bytes = await Client(handler).DownloadAsync(PictureUrl, TestContext.Current.CancellationToken);

        bytes.ShouldBeEmpty();
        handler.Requests.ShouldHaveSingleItem().Authorization.ShouldBeNull();
    }

    [Fact]
    public async Task DownloadAsync_Picture_ReturnsItsBytesWithoutTheToken()
    {
        var image = TestImages.Jpeg(10, 10);
        var handler = new FixtureHandler().Bytes(PictureUrl, image);

        var bytes = await Client(handler).DownloadAsync(PictureUrl, TestContext.Current.CancellationToken);

        bytes.ShouldBe(image);
        handler.Requests.ShouldHaveSingleItem().Authorization.ShouldBeNull();
    }

    public void Dispose()
    {
        foreach (var client in _clients)
        {
            client.Dispose();
        }
    }

    private MercadoLivreClient Client(FixtureHandler handler)
    {
        var http = new HttpClient(handler, disposeHandler: false);
        _clients.Add(http);
        return new MercadoLivreClient(http, "test-token", ImportCommand.DefaultRetryDelay, (delay, _) =>
        {
            _waits.Add(delay);
            return Task.CompletedTask;
        });
    }
}
