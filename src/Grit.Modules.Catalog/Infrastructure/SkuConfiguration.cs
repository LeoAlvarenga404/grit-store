using Grit.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grit.Modules.Catalog.Infrastructure;

internal sealed class SkuConfiguration : IEntityTypeConfiguration<Sku>
{
    public void Configure(EntityTypeBuilder<Sku> builder)
    {
        builder.ToTable("skus");
        builder.HasKey(sku => sku.Id);
        builder.Property(sku => sku.Id)
            .HasConversion(id => id.Value, value => new SkuId(value))
            .ValueGeneratedNever();
        builder.Property(sku => sku.Attributes).IsJsonAttributes();
        builder.ComplexProperty(sku => sku.Price, price =>
        {
            price.Property(money => money.Amount).HasPrecision(15, 2);
            price.Property(money => money.Currency).HasMaxLength(3).IsFixedLength();
        });
    }
}
