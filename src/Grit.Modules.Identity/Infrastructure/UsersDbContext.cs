using Grit.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Grit.Modules.Identity.Infrastructure;

internal sealed class UsersDbContext(DbContextOptions<UsersDbContext> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options)
{
    public const string Schema = "identity";

    private static readonly Guid CustomerRoleId = new("01990000-0000-7000-8000-000000000001");

    private static readonly Guid AdminRoleId = new("01990000-0000-7000-8000-000000000002");

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, NpgsqlDataSource dataSource) =>
        options
            .UseNpgsql(dataSource, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", Schema))
            .UseSnakeCaseNamingConvention();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);

        // Identity names its tables explicitly ("AspNetUsers", ...), which the snake_case convention leaves alone.
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("roles").HasData(Role(CustomerRoleId, Domain.Roles.Customer), Role(AdminRoleId, Domain.Roles.Admin));
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly);
    }

    // Fixed id and stamp: a value that changed per build would make every new migration re-seed the roles.
    private static IdentityRole<Guid> Role(Guid id, string name) =>
        new() { Id = id, Name = name, NormalizedName = name.ToUpperInvariant(), ConcurrencyStamp = name };
}
