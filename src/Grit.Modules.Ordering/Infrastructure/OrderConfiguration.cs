using Grit.Modules.Ordering.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grit.Modules.Ordering.Infrastructure;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id)
            .HasConversion(id => id.Value, value => new OrderId(value))
            .ValueGeneratedNever();
        builder.Property(order => order.Number).HasMaxLength(20);
        builder.HasIndex(order => order.Number).IsUnique();
        builder.Property(order => order.IdempotencyKey).HasMaxLength(100);
        builder.HasIndex(order => new { order.CustomerId, order.IdempotencyKey }).IsUnique();
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(order => order.CancellationReason).HasMaxLength(50);
        builder.ComplexProperty(order => order.Subtotal, MoneyMapping.Configure);
        builder.ComplexProperty(order => order.ShippingCost, MoneyMapping.Configure);
        builder.ComplexProperty(order => order.Total, MoneyMapping.Configure);
        builder.HasMany(order => order.Lines).WithOne().HasForeignKey("OrderId").IsRequired();
        builder.HasMany(order => order.History).WithOne().HasForeignKey("OrderId").IsRequired();
    }
}
