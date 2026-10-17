using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Grit.Modules.Payments.Infrastructure;

internal sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public const string Schema = "payments";

    public DbSet<StripeEvent> StripeEvents => Set<StripeEvent>();

    public static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, NpgsqlDataSource dataSource) =>
        options
            .UseNpgsql(dataSource, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", Schema))
            .UseSnakeCaseNamingConvention();

    // Stripe delivers an event at least once; the key on its id lets one delivery in, even when two arrive at once.
    // Returns whether this delivery was the first.
    public async Task<bool> RecordStripeEventAsync(string eventId, string type, CancellationToken cancellationToken) =>
        await Database.ExecuteSqlAsync(
            $"INSERT INTO payments.stripe_events (event_id, type, received_at) VALUES ({eventId}, {type}, now()) ON CONFLICT (event_id) DO NOTHING",
            cancellationToken) == 1;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<StripeEvent>().HasKey(stripeEvent => stripeEvent.EventId);
    }
}
