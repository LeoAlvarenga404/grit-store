using Grit.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Grit.Modules.Identity.Infrastructure;

// Development only: the AppHost generates the password as a secret parameter, so no admin credential is ever committed.
internal sealed class DevelopmentAdminSeeder(IServiceScopeFactory scopes, IConfiguration configuration) : IHostedService
{
    public const string EmailKey = "Identity:Admin:Email";

    public const string PasswordKey = "Identity:Admin:Password";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var email = configuration[EmailKey] ?? throw new InvalidOperationException($"{EmailKey} is required when {PasswordKey} is set.");
        await using var scope = scopes.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        // An existing admin keeps its password; the AppHost's Postgres has no volume, so each run starts from an empty database.
        var admin = await users.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new User { Id = Guid.CreateVersion7(), UserName = email, Email = email };
            ThrowIfFailed(await users.CreateAsync(admin, configuration[PasswordKey]!));
        }

        if (!await users.IsInRoleAsync(admin, Roles.Admin))
        {
            ThrowIfFailed(await users.AddToRoleAsync(admin, Roles.Admin));
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static void ThrowIfFailed(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}")));
        }
    }
}
