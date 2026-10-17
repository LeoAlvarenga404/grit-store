using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Grit.IntegrationTests;

// TestServer leaves the caller's address empty, and the forwarded-headers middleware trusts an empty address,
// so every request here sets it through the handler. Each test has a host of its own that allows one login a minute.
public class IdentityLogInForwardedForTests(ApiFactory factory)
{
    private const string Loopback = "127.0.0.1";

    private const string ConfiguredProxy = "198.51.100.7";

    private const string UntrustedCaller = "192.0.2.7";

    [Fact]
    public async Task LogIn_ThroughTheLoopbackProxy_LimitsEachForwardedClientSeparately()
    {
        using var host = LimitedHost();

        (await LogInAsync(host, Loopback, "203.0.113.10")).ShouldBe(StatusCodes.Status401Unauthorized);
        (await LogInAsync(host, Loopback, "203.0.113.11")).ShouldBe(StatusCodes.Status401Unauthorized);
        (await LogInAsync(host, Loopback, "203.0.113.10")).ShouldBe(StatusCodes.Status429TooManyRequests);
    }

    [Fact]
    public async Task LogIn_ThroughAProxyInAConfiguredNetwork_LimitsEachForwardedClientSeparately()
    {
        using var host = LimitedHost();

        (await LogInAsync(host, ConfiguredProxy, "203.0.113.20")).ShouldBe(StatusCodes.Status401Unauthorized);
        (await LogInAsync(host, ConfiguredProxy, "203.0.113.21")).ShouldBe(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task LogIn_FromAnUntrustedCaller_IgnoresTheForwardedAddress()
    {
        using var host = LimitedHost();

        (await LogInAsync(host, UntrustedCaller, "203.0.113.30")).ShouldBe(StatusCodes.Status401Unauthorized);
        (await LogInAsync(host, UntrustedCaller, "203.0.113.31")).ShouldBe(StatusCodes.Status429TooManyRequests);
    }

    private WebApplicationFactory<Program> LimitedHost() =>
        factory.WithWebHostBuilder(builder => builder
            .UseSetting("Identity:LoginAttemptsPerMinute", "1")
            .UseSetting("ForwardedHeaders:KnownNetworks:0", "198.51.100.0/24"));

    private static async Task<int> LogInAsync(WebApplicationFactory<Program> host, string callerAddress, string forwardedFor)
    {
        using var client = new HttpClient(host.Server.CreateHandler(context => context.Connection.RemoteIpAddress = IPAddress.Parse(callerAddress)))
        {
            BaseAddress = host.Server.BaseAddress,
        };
        client.DefaultRequestHeaders.Add("X-Forwarded-For", forwardedFor);

        using var response = await IdentityApi.LogInAsync(client, IdentityApi.UniqueEmail(), IdentityApi.Password);
        return (int)response.StatusCode;
    }
}
