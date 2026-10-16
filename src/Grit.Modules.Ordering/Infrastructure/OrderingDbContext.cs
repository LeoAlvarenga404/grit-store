using Grit.Modules.Ordering.Domain;
using Grit.Modules.Ordering.Features.Checkout;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Grit.Modules.Ordering.Infrastructure;

internal sealed class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    public const string Schema = "ordering";

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<CheckoutState> CheckoutStates => Set<CheckoutState>();

    public static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, NpgsqlDataSource dataSource) =>
        options
            .UseNpgsql(dataSource, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", Schema))
            .UseSnakeCaseNamingConvention();

    // A sequence never rolls back, so order numbers can skip values; they are readable, not contiguous.
    public async Task<long> NextOrderSequenceAsync(CancellationToken cancellationToken)
    {
        await Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT nextval('ordering.order_numbers')";
            return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
        }
        finally
        {
            await Database.CloseConnectionAsync();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.HasSequence<long>("order_numbers");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderingDbContext).Assembly);
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
