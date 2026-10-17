using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Grit.IntegrationTests;

// SeaweedFS with one S3 identity, like the AppHost's: only the product image tests pay for its start (about 6 s).
public sealed class ProductImageHost(ApiFactory api) : IAsyncLifetime
{
    private const string AccessKey = "grit-test";

    private const string SecretKey = "test-only-secret-0123456789";

    private const int S3Port = 8333;

    private readonly IContainer _storage = new ContainerBuilder("chrislusf/seaweedfs:4.46")
        .WithResourceMapping(
            Encoding.UTF8.GetBytes($$"""{"identities":[{"name":"grit","credentials":[{"accessKey":"{{AccessKey}}","secretKey":"{{SecretKey}}"}],"actions":["Admin","Read","Write","List"]}]}"""),
            "/etc/seaweedfs/s3.json")
        .WithCommand("server", "-dir=/data", "-s3", "-s3.config=/etc/seaweedfs/s3.json")
        .WithTmpfsMount("/data")
        .WithPortBinding(S3Port, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Start Seaweed S3 API Server"))
        .Build();

    private WebApplicationFactory<Program>? _host;

    public WebApplicationFactory<Program> Host => _host ?? throw new InvalidOperationException("The image host has not started.");

    public async ValueTask InitializeAsync()
    {
        await _storage.StartAsync();
        var endpoint = $"http://{_storage.Hostname}:{_storage.GetMappedPublicPort(S3Port)}";
        _host = api.WithWebHostBuilder(builder => builder.UseSetting("ConnectionStrings:storage", $"Endpoint={endpoint};AccessKey={AccessKey};SecretKey={SecretKey}"));

        // Starting the host runs the start-up provisioning, which creates the bucket.
        _ = _host.Services;
    }

    public async ValueTask DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.DisposeAsync();
        }

        await _storage.DisposeAsync();
    }
}
