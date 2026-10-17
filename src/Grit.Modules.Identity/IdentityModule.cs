using System.Threading.RateLimiting;
using Grit.BuildingBlocks;
using Grit.Modules.Identity.Features.GetCurrentUser;
using Grit.Modules.Identity.Features.LogIn;
using Grit.Modules.Identity.Features.LogOut;
using Grit.Modules.Identity.Features.RefreshSession;
using Grit.Modules.Identity.Features.Register;
using Grit.Modules.Identity.Infrastructure;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Grit.Modules.Identity;

public sealed class IdentityModule : IModule
{
    public void AddServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddDbContext<UsersDbContext>((services, options) =>
            UsersDbContext.Configure(options, services.GetRequiredService<NpgsqlDataSource>()));
        builder.Services.AddIdentityCore<User>(ConfigureIdentity)
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<UsersDbContext>();
        builder.Services.AddScoped<TokenIssuer>();

        builder.Services.AddOptions<JwtOptions>().BindConfiguration(JwtOptions.Section);
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) => jwt.Value.ConfigureBearer(bearer));
        builder.Services.AddAuthorization();

        AddLogInRateLimit(builder);

        // Development only, and only when the AppHost passes the generated password (ADR 0008 keeps seeds out of production).
        if (builder.Environment.IsDevelopment() && !string.IsNullOrEmpty(builder.Configuration[DevelopmentAdminSeeder.PasswordKey]))
        {
            builder.Services.AddHostedService<DevelopmentAdminSeeder>();
        }
    }

    // Identity publishes no integration event, and the single bus-outbox slot stays with the probe (ADR 0005).
    public void AddMessaging(IBusRegistrationConfigurator bus)
    {
    }

    public async Task MigrateDatabaseAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<UsersDbContext>().Database.MigrateAsync(cancellationToken);
    }

    public void MapEndpoints(IEndpointRouteBuilder api)
    {
        var identity = api.MapGroup("/identity").WithTags("Identity");
        identity.MapRegister();
        identity.MapLogIn();
        identity.MapRefreshSession();
        identity.MapLogOut();
        identity.MapGetCurrentUser();
    }

    private static void ConfigureIdentity(IdentityOptions options)
    {
        options.User.RequireUniqueEmail = true;
        // The user name is the e-mail, whose format the validator already checks; any character it may carry is allowed.
        options.User.AllowedUserNameCharacters = string.Empty;
        // Length over composition rules (NIST SP 800-63B); the validators enforce 8 to 128 characters.
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    }

    // Behind the BFF the client address comes from X-Forwarded-For, which Program.cs accepts only from known proxies.
    private static void AddLogInRateLimit(IHostApplicationBuilder builder)
    {
        var attemptsPerMinute = builder.Configuration.GetValue("Identity:LoginAttemptsPerMinute", 10);
        builder.Services.Configure<RateLimiterOptions>(options => options.AddPolicy(LogInEndpoint.RateLimitPolicy, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = attemptsPerMinute, Window = TimeSpan.FromMinutes(1) })));
    }
}
