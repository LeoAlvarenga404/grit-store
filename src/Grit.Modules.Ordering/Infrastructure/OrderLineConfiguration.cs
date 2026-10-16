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
        builder.Property(line => line.ProductName);
        builder.Property(line => line.Quantity);
        builder.ComplexProperty(line => line.UnitPrice, MoneyMapping.Configure);
        builder.Ignore(line => line.LineTotal);
    }
}
