using System.Net;

namespace Grit.MlImporter.Tests;

// The command line around the importer: exit codes, the token and the retries.
public sealed class ImportCommandTests : IDisposable
{
    private readonly string _output = Path.Combine(Path.GetTempPath(), $"grit-ml-{Guid.NewGuid():N}");

    private string[] Arguments => ["--category", RecordedCatalog.Category, "--count", "2", "--output", _output];

    [Fact]
    public async Task RunAsync_RecordedFamilies_SendsTheTokenToTheApiOnly()
    {
        var handler = RecordedCatalog.Handler();

        var exitCode = await RunAsync(handler, "test-token", TextWriter.Null);

        exitCode.ShouldBe(ImportCommand.Succeeded);
        handler.Requests.Where(request => request.Url.StartsWith(RecordedCatalog.Api, StringComparison.Ordinal))
            .ShouldAllBe(request => request.Authorization == "Bearer test-token");
        handler.Requests.Where(request => request.Url.StartsWith(RecordedCatalog.Picture, StringComparison.Ordinal))
            .ShouldAllBe(request => request.Authorization == null);
        handler.Requests.Count(request => request.Url.StartsWith(RecordedCatalog.Picture, StringComparison.Ordinal)).ShouldBe(6);
    }

    [Fact]
    public async Task RunAsync_NoToken_Returns2WithTheUserSecretsCommandAndCallsNothing()
    {
        var handler = new FixtureHandler();
        var error = new StringWriter();

        var exitCode = await ImportCommand.RunAsync(Arguments, " ", handler, TextWriter.Null, error, retryDelay: TimeSpan.Zero);

        exitCode.ShouldBe(ImportCommand.MissingToken);
        error.ToString().ShouldContain("dotnet user-secrets set \"MercadoLivre:AccessToken\"");
        handler.Requests.ShouldBeEmpty();
        Directory.Exists(_output).ShouldBeFalse();
    }

    [Fact]
    public async Task RunAsync_BadArguments_Returns1WithTheUsage()
    {
        var error = new StringWriter();

        var exitCode = await ImportCommand.RunAsync(["--count", "3"], "test-token", new FixtureHandler(), TextWriter.Null, error, retryDelay: TimeSpan.Zero);

        exitCode.ShouldBe(ImportCommand.InvalidArguments);
        error.ToString().ShouldContain("Usage: dotnet run --project tools/Grit.MlImporter");
    }

    [Fact]
    public async Task RunAsync_TokenRejected_Returns3AndWritesNothing()
    {
        var handler = new FixtureHandler().Json(RecordedCatalog.FirstPage, "items-not-found.json", HttpStatusCode.Unauthorized);
        var error = new StringWriter();

        var exitCode = await RunAsync(handler, "expired-token", error);

        exitCode.ShouldBe(ImportCommand.RequestFailed);
        error.ToString().ShouldContain("401");
        error.ToString().ShouldContain("/products/search?");
        Directory.Exists(_output).ShouldBeFalse();
    }

    [Fact]
    public async Task RunAsync_ThrottledTwice_RetriesAndSucceeds()
    {
        var handler = RecordedCatalog.Handler().Throttled(RecordedCatalog.FirstPage, 2);

        var exitCode = await RunAsync(handler, "test-token", TextWriter.Null);

        exitCode.ShouldBe(ImportCommand.Succeeded);
        handler.Requests.Count(request => request.Url == RecordedCatalog.FirstPage).ShouldBe(3);
    }

    [Fact]
    public async Task RunAsync_ThrottledEveryTime_Returns3NamingTheUrl()
    {
        var handler = RecordedCatalog.Handler().Throttled(RecordedCatalog.FirstPage, 10);
        var error = new StringWriter();

        var exitCode = await RunAsync(handler, "test-token", error);

        exitCode.ShouldBe(ImportCommand.RequestFailed);
        handler.Requests.Count(request => request.Url == RecordedCatalog.FirstPage).ShouldBe(MercadoLivreClient.MaxAttempts);
        error.ToString().ShouldContain("429");
        error.ToString().ShouldContain("/products/search?");
        Directory.Exists(_output).ShouldBeFalse();
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task RunAsync_ServerErrorTwice_RetriesAndSucceeds(HttpStatusCode status)
    {
        var handler = RecordedCatalog.Handler().Failing(RecordedCatalog.FirstPage, 2, status);

        var exitCode = await RunAsync(handler, "test-token", TextWriter.Null);

        exitCode.ShouldBe(ImportCommand.Succeeded);
        handler.Requests.Count(request => request.Url == RecordedCatalog.FirstPage).ShouldBe(3);
    }

    public void Dispose()
    {
        if (Directory.Exists(_output))
        {
            Directory.Delete(_output, recursive: true);
        }
    }

    private Task<int> RunAsync(FixtureHandler handler, string token, TextWriter error) =>
        ImportCommand.RunAsync(Arguments, token, handler, TextWriter.Null, error, retryDelay: TimeSpan.Zero);
}
