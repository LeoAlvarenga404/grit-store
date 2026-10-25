using System.Net;
using System.Net.Http.Json;
using Grit.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Grit.IntegrationTests;

public class IdentityRegisterTests(ApiFactory factory)
{
    private const int Rounds = 5;

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_NewEmail_Returns201AndCreatesACustomerWithAHashedPassword()
    {
        var (id, email) = await IdentityApi.RegisterAsync(_client);

        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await users.FindByIdAsync(id.ToString());
        user.ShouldNotBeNull();
        user.Email.ShouldBe(email);
        user.PasswordHash.ShouldNotBeNullOrEmpty();
        user.PasswordHash.ShouldNotContain(IdentityApi.Password);
        (await users.GetRolesAsync(user)).ShouldHaveSingleItem().ShouldBe("customer");
    }

    [Fact]
    public async Task Register_EmailTakenInAnotherCase_Returns409EmailTaken()
    {
        var (_, email) = await IdentityApi.RegisterAsync(_client);

        var response = await _client.PostAsJsonAsync(
            IdentityApi.RegisterPath,
            new { email = email.ToUpperInvariant(), password = IdentityApi.Password },
            TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.Conflict);
        problem.GetProperty("code").GetString().ShouldBe("identity.email_taken");
    }

    // Both requests pass Identity's duplicate check before either commits; several rounds make the race near certain.
    [Fact]
    public async Task Register_SameEmailTwiceAtOnce_Returns201And409()
    {
        for (var round = 0; round < Rounds; round++)
        {
            var body = new { email = IdentityApi.UniqueEmail(), password = IdentityApi.Password };

            var responses = await Task.WhenAll(
                _client.PostAsJsonAsync(IdentityApi.RegisterPath, body, TestContext.Current.CancellationToken),
                _client.PostAsJsonAsync(IdentityApi.RegisterPath, body, TestContext.Current.CancellationToken));

            responses.Select(response => response.StatusCode).Order().ShouldBe([HttpStatusCode.Created, HttpStatusCode.Conflict]);
            var conflict = responses.Single(response => response.StatusCode == HttpStatusCode.Conflict);
            var problem = await ProblemResponse.ReadAsync(conflict, HttpStatusCode.Conflict);
            problem.GetProperty("code").GetString().ShouldBe("identity.email_taken");
        }
    }

    // A password has a lowercase letter, an uppercase letter, a digit and a symbol; 8 to 128 characters (ADR 0014 amendment).
    private static string ComposedPassword(int length) => "Aa1!" + new string('a', length - 4);

    [Theory]
    [InlineData(8)]
    [InlineData(128)]
    public async Task Register_ComposedPasswordAtEitherLengthLimit_Returns201(int length)
    {
        var response = await _client.PostAsJsonAsync(
            IdentityApi.RegisterPath,
            new { email = IdentityApi.UniqueEmail(), password = ComposedPassword(length) },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Theory]
    [InlineData("SENHA-FORTE-123!")]
    [InlineData("senha-forte-123!")]
    [InlineData("Senha-forte-sem-numero!")]
    [InlineData("Senhaforte123")]
    [InlineData("senhafraca")]
    public async Task Register_PasswordMissingACharacterClass_Returns400WithThePasswordField(string password)
    {
        var response = await _client.PostAsJsonAsync(
            IdentityApi.RegisterPath,
            new { email = IdentityApi.UniqueEmail(), password },
            TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
        problem.GetProperty("errors").TryGetProperty("password", out _).ShouldBeTrue();
    }

    [Theory]
    [InlineData("nao-e-um-email", 18, "email")]
    [InlineData("", 18, "email")]
    [InlineData("ana\0@grit.dev", 18, "email")]
    [InlineData("ana@grit.dev", 7, "password")]
    [InlineData("ana@grit.dev", 129, "password")]
    public async Task Register_InvalidInput_Returns400WithTheField(string email, int passwordLength, string field)
    {
        var response = await _client.PostAsJsonAsync(
            IdentityApi.RegisterPath,
            new { email, password = ComposedPassword(passwordLength) },
            TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
        problem.GetProperty("errors").TryGetProperty(field, out _).ShouldBeTrue();
    }
}
