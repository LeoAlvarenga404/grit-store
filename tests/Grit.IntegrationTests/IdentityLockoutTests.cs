using System.Net;
using Grit.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.IntegrationTests;

public class IdentityLockoutTests(ApiFactory factory)
{
    private const string WrongPassword = "senha-errada-123";

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task LogIn_FifthWrongPassword_LocksTheAccountFor15Minutes()
    {
        var (id, email) = await IdentityApi.RegisterAsync(_client);

        await FailLogInAsync(email, times: 5);

        var lockoutEnd = (await FindUserAsync(id)).LockoutEnd;
        lockoutEnd.ShouldNotBeNull();
        lockoutEnd.Value.ShouldBe(DateTimeOffset.UtcNow.AddMinutes(15), TimeSpan.FromMinutes(1));
    }

    // Moving LockoutEnd into the past stands in for waiting 15 minutes.
    [Fact]
    public async Task LogIn_AfterTheLockoutEnds_AcceptsTheRightPassword()
    {
        var (id, email) = await IdentityApi.RegisterAsync(_client);
        await FailLogInAsync(email, times: 5);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await users.FindByIdAsync(id.ToString());
            user.ShouldNotBeNull();
            (await users.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddSeconds(-1))).Succeeded.ShouldBeTrue();
        }

        var tokens = await IdentityApi.LogInAsync(_client, email);

        tokens.GetProperty("accessToken").GetString().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task LogIn_RightPasswordAfterFourWrongOnes_ResetsTheFailureCount()
    {
        var (id, email) = await IdentityApi.RegisterAsync(_client);
        await FailLogInAsync(email, times: 4);

        await IdentityApi.LogInAsync(_client, email);

        (await FindUserAsync(id)).AccessFailedCount.ShouldBe(0);
        await IdentityApi.AssertUnauthorizedAsync(await IdentityApi.LogInAsync(_client, email, WrongPassword), "identity.invalid_credentials");
    }

    private async Task FailLogInAsync(string email, int times)
    {
        for (var attempt = 1; attempt <= times; attempt++)
        {
            (await IdentityApi.LogInAsync(_client, email, WrongPassword)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }

    private async Task<User> FindUserAsync(Guid id)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var user = await scope.ServiceProvider.GetRequiredService<UserManager<User>>().FindByIdAsync(id.ToString());
        user.ShouldNotBeNull();
        return user;
    }
}
