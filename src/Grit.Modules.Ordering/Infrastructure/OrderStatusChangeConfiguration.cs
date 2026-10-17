using Grit.Modules.Ordering.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grit.Modules.Ordering.Infrastructure;

internal sealed class OrderStatusChangeConfiguration : IEntityTypeConfiguration<OrderStatusChange>
{
    public void Configure(EntityTypeBuilder<OrderStatusChange> builder)
    {
        builder.ToTable("order_status_changes");
        builder.Property<int>("Id");
        builder.HasKey("Id");
        builder.Property(change => change.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(change => change.OccurredAt);
    }
}
