using Grit.Modules.Ordering.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grit.Modules.Ordering.Infrastructure;

internal sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("order_lines");
        builder.Property<int>("Id");
        builder.HasKey("Id");
        builder.Property(line => line.SkuId);
        // Same bound as the catalog's product name, which the line copies at checkout.
        builder.Property(line => line.ProductName).HasMaxLength(200);
        // The catalog bounds slugs and SKU attribute values at 100 characters.
        builder.Property(line => line.ProductSlug).HasMaxLength(100);
        builder.Property(line => line.Color).HasMaxLength(100);
        builder.Property(line => line.Size).HasMaxLength(100);
        builder.Property(line => line.Quantity);
        builder.ComplexProperty(line => line.UnitPrice, MoneyMapping.Configure);
        builder.Ignore(line => line.LineTotal);
    }
}
