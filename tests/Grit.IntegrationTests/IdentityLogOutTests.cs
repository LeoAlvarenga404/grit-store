using System.Net;
using System.Net.Http.Json;
using Grit.Modules.Identity.Domain;
using Grit.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.IntegrationTests;

public class IdentityLogOutTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task LogOut_ActiveToken_Returns204AndTheTokenNoLongerRefreshes()
    {
        var token = IdentityApi.RefreshTokenOf(await IdentityApi.StartSessionAsync(_client));

        (await IdentityApi.LogOutAsync(_client, token)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await IdentityApi.AssertUnauthorizedAsync(await IdentityApi.RefreshAsync(_client, token), "identity.invalid_refresh_token");
    }

    [Fact]
    public async Task LogOut_ReplacedTokenOfTheSession_EndsTheCurrentTokenToo()
    {
        var first = IdentityApi.RefreshTokenOf(await IdentityApi.StartSessionAsync(_client));
        var second = IdentityApi.RefreshTokenOf(await IdentityApi.ReadOkAsync(await IdentityApi.RefreshAsync(_client, first)));

        (await IdentityApi.LogOutAsync(_client, first)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await IdentityApi.AssertUnauthorizedAsync(await IdentityApi.RefreshAsync(_client, second), "identity.invalid_refresh_token");
    }

    [Fact]
    public async Task LogOut_OneSession_LeavesTheOtherSessionsOfTheUserWorking()
    {
        var (_, email) = await IdentityApi.RegisterAsync(_client);
        var loggedOut = IdentityApi.RefreshTokenOf(await IdentityApi.LogInAsync(_client, email));
        var otherDevice = IdentityApi.RefreshTokenOf(await IdentityApi.LogInAsync(_client, email));

        (await IdentityApi.LogOutAsync(_client, loggedOut)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await IdentityApi.RefreshAsync(_client, otherDevice)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogOut_UnknownToken_Returns204()
    {
        var response = await IdentityApi.LogOutAsync(_client, "token-que-nao-existe");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task LogOut_SameTokenTwice_Returns204BothTimes()
    {
        var token = IdentityApi.RefreshTokenOf(await IdentityApi.StartSessionAsync(_client));
        (await IdentityApi.LogOutAsync(_client, token)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var second = await IdentityApi.LogOutAsync(_client, token);

        second.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task LogOut_EmptyToken_Returns400WithTheField()
    {
        var response = await _client.PostAsJsonAsync(IdentityApi.LogOutPath, new { refreshToken = "" }, TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
        problem.GetProperty("errors").TryGetProperty("refreshToken", out _).ShouldBeTrue();
    }

    // Stands in for a rotation of the presented token that has not committed yet: the test transaction takes the family lock,
    // replaces the token and inserts its successor.
    [Fact]
    public async Task LogOut_WhileARotationIsInFlight_RevokesTheSuccessor()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (id, email) = await IdentityApi.RegisterAsync(_client);
        var token = IdentityApi.RefreshTokenOf(await IdentityApi.LogInAsync(_client, email));
        var familyId = await IdentityApi.FamilyIdOfAsync(factory.Services, token);
        var hash = RefreshToken.Hash(token);

        await using var rotation = factory.Services.CreateAsyncScope();
        var rotationDb = rotation.ServiceProvider.GetRequiredService<UsersDbContext>();
        await using var transaction = await rotationDb.Database.BeginTransactionAsync(cancellationToken);
        await rotationDb.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock(hashtextextended({familyId.ToString()}, 0))", cancellationToken);
        await rotationDb.RefreshTokens
            .Where(row => row.TokenHash == hash)
            .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.ReplacedAt, DateTimeOffset.UtcNow), cancellationToken);
        var successor = RefreshToken.Issue(id, familyId, DateTimeOffset.UtcNow).Token;
        rotationDb.RefreshTokens.Add(successor);
        await rotationDb.SaveChangesAsync(cancellationToken);

        var logOut = IdentityApi.LogOutAsync(_client, token);
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        logOut.IsCompleted.ShouldBeFalse();

        await transaction.CommitAsync(cancellationToken);
        (await logOut).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await using var check = factory.Services.CreateAsyncScope();
        var stored = await check.ServiceProvider.GetRequiredService<UsersDbContext>().RefreshTokens
            .SingleAsync(row => row.Id == successor.Id, cancellationToken);
        stored.RevokedAt.ShouldNotBeNull();
    }
}
