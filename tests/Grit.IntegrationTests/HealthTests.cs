using System.Net;
using System.Net.Http.Json;
using Grit.ServiceDefaults;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Grit.IntegrationTests;

public class HealthTests(ApiFactory factory)
{
    [Fact]
    public async Task Health_DatabaseReachable_ReturnsHealthy()
    {
        var response = await factory.CreateClient().GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var health = await response.Content.ReadFromJsonAsync<HealthResponse>(TestContext.Current.CancellationToken);
        health!.Status.ShouldBe("Healthy");
    }

    [Fact]
    public async Task Health_FailingCheck_Returns503Unhealthy()
    {
        await using var unhealthyFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(
            services => services.AddHealthChecks().AddCheck("failing", () => HealthCheckResult.Unhealthy())));

        var response = await unhealthyFactory.CreateClient().GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        var health = await response.Content.ReadFromJsonAsync<HealthResponse>(TestContext.Current.CancellationToken);
        health!.Status.ShouldBe("Unhealthy");
    }
}
