using System.Net;
using System.Net.Http.Json;
using Grit.Modules.Identity.Domain;
using Grit.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.IntegrationTests;

public class IdentityRefreshTests(ApiFactory factory)
{
    private const int Rounds = 5;

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Refresh_ActiveToken_ReturnsANewPairWhoseAccessTokenReadsMe()
    {
        var first = IdentityApi.RefreshTokenOf(await IdentityApi.StartSessionAsync(_client));

        var renewed = await IdentityApi.ReadOkAsync(await IdentityApi.RefreshAsync(_client, first));

        IdentityApi.RefreshTokenOf(renewed).ShouldNotBe(first);
        var me = await IdentityApi.GetMeAsync(_client, renewed.GetProperty("accessToken").GetString()!);
        me.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_ReplacedToken_Returns401ReusedAndRevokesTheWholeFamily()
    {
        var first = IdentityApi.RefreshTokenOf(await IdentityApi.StartSessionAsync(_client));
        var second = IdentityApi.RefreshTokenOf(await IdentityApi.ReadOkAsync(await IdentityApi.RefreshAsync(_client, first)));

        await IdentityApi.AssertUnauthorizedAsync(await IdentityApi.RefreshAsync(_client, first), "identity.refresh_token_reused");

        await IdentityApi.AssertUnauthorizedAsync(await IdentityApi.RefreshAsync(_client, second), "identity.invalid_refresh_token");
    }

    [Fact]
    public async Task Refresh_ReuseInOneSession_LeavesTheOtherSessionsOfTheUserWorking()
    {
        var (_, email) = await IdentityApi.RegisterAsync(_client);
        var replayed = IdentityApi.RefreshTokenOf(await IdentityApi.LogInAsync(_client, email));
        var otherDevice = IdentityApi.RefreshTokenOf(await IdentityApi.LogInAsync(_client, email));
        await IdentityApi.ReadOkAsync(await IdentityApi.RefreshAsync(_client, replayed));

        await IdentityApi.AssertUnauthorizedAsync(await IdentityApi.RefreshAsync(_client, replayed), "identity.refresh_token_reused");

        (await IdentityApi.RefreshAsync(_client, otherDevice)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // Both requests carry one token: the conditional UPDATE lets exactly one rotate, and the other counts as reuse.
    [Fact]
    public async Task Refresh_SameTokenTwiceAtOnce_OneRotatesAndTheFamilyIsRevoked()
    {
        for (var round = 0; round < Rounds; round++)
        {
            var token = IdentityApi.RefreshTokenOf(await IdentityApi.StartSessionAsync(_client));

            var responses = await Task.WhenAll(IdentityApi.RefreshAsync(_client, token), IdentityApi.RefreshAsync(_client, token));

            responses.Select(response => response.StatusCode).Order().ShouldBe([HttpStatusCode.OK, HttpStatusCode.Unauthorized]);
            await IdentityApi.AssertUnauthorizedAsync(
                responses.Single(response => response.StatusCode == HttpStatusCode.Unauthorized),
                "identity.refresh_token_reused");
            var winner = await IdentityApi.ReadOkAsync(responses.Single(response => response.StatusCode == HttpStatusCode.OK));
            await IdentityApi.AssertUnauthorizedAsync(
                await IdentityApi.RefreshAsync(_client, IdentityApi.RefreshTokenOf(winner)),
                "identity.invalid_refresh_token");
        }
    }

    [Fact]
    public async Task Refresh_UnknownToken_Returns401Invalid()
    {
        var response = await IdentityApi.RefreshAsync(_client, "token-que-nao-existe");

        await IdentityApi.AssertUnauthorizedAsync(response, "identity.invalid_refresh_token");
    }

    [Fact]
    public async Task Refresh_ExpiredToken_Returns401InvalidAndRevokesNothing()
    {
        var (id, email) = await IdentityApi.RegisterAsync(_client);
        var token = IdentityApi.RefreshTokenOf(await IdentityApi.LogInAsync(_client, email));
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<UsersDbContext>().RefreshTokens
                .Where(stored => stored.UserId == id)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(stored => stored.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1)),
                    TestContext.Current.CancellationToken);
        }

        await IdentityApi.AssertUnauthorizedAsync(await IdentityApi.RefreshAsync(_client, token), "identity.invalid_refresh_token");

        await using var check = factory.Services.CreateAsyncScope();
        var stored = await check.ServiceProvider.GetRequiredService<UsersDbContext>().RefreshTokens
            .SingleAsync(row => row.UserId == id, TestContext.Current.CancellationToken);
        stored.RevokedAt.ShouldBeNull();
        stored.ReplacedAt.ShouldBeNull();
    }

    // Stands in for a rotation of B that has not committed yet: the test transaction takes the family lock, replaces B and inserts its successor C.
    [Fact]
    public async Task Refresh_AncestorReusedWhileSuccessorRotationIsInFlight_RevokesTheSuccessor()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var (id, email) = await IdentityApi.RegisterAsync(_client);
        var first = IdentityApi.RefreshTokenOf(await IdentityApi.LogInAsync(_client, email));
        var second = IdentityApi.RefreshTokenOf(await IdentityApi.ReadOkAsync(await IdentityApi.RefreshAsync(_client, first)));
        var familyId = await FamilyIdOfAsync(second);
        var secondHash = RefreshToken.Hash(second);

        await using var rotation = factory.Services.CreateAsyncScope();
        var rotationDb = rotation.ServiceProvider.GetRequiredService<UsersDbContext>();
        await using var transaction = await rotationDb.Database.BeginTransactionAsync(cancellationToken);
        await rotationDb.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock(hashtextextended({familyId.ToString()}, 0))", cancellationToken);
        await rotationDb.RefreshTokens
            .Where(token => token.TokenHash == secondHash)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.ReplacedAt, DateTimeOffset.UtcNow), cancellationToken);
        var successor = RefreshToken.Issue(id, familyId, DateTimeOffset.UtcNow).Token;
        rotationDb.RefreshTokens.Add(successor);
        await rotationDb.SaveChangesAsync(cancellationToken);

        var reuse = IdentityApi.RefreshAsync(_client, first);
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        reuse.IsCompleted.ShouldBeFalse();

        await transaction.CommitAsync(cancellationToken);
        await IdentityApi.AssertUnauthorizedAsync(await reuse, "identity.refresh_token_reused");

        await using var check = factory.Services.CreateAsyncScope();
        var stored = await check.ServiceProvider.GetRequiredService<UsersDbContext>().RefreshTokens
            .SingleAsync(row => row.Id == successor.Id, cancellationToken);
        stored.RevokedAt.ShouldNotBeNull();
    }

    private async Task<Guid> FamilyIdOfAsync(string refreshToken)
    {
        var hash = RefreshToken.Hash(refreshToken);
        await using var scope = factory.Services.CreateAsyncScope();
        var stored = await scope.ServiceProvider.GetRequiredService<UsersDbContext>().RefreshTokens
            .SingleAsync(row => row.TokenHash == hash, TestContext.Current.CancellationToken);
        return stored.FamilyId;
    }

    [Fact]
    public async Task Refresh_EmptyToken_Returns400WithTheField()
    {
        var response = await _client.PostAsJsonAsync(IdentityApi.RefreshPath, new { refreshToken = "" }, TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
        problem.GetProperty("errors").TryGetProperty("refreshToken", out _).ShouldBeTrue();
    }
}
