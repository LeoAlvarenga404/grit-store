using Grit.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grit.Modules.Catalog.Infrastructure;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id)
            .HasConversion(id => id.Value, value => new ProductId(value))
            .ValueGeneratedNever();
        builder.Property(product => product.CategoryId).HasConversion(id => id.Value, value => new CategoryId(value));
        builder.HasOne<Category>().WithMany().HasForeignKey(product => product.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(product => product.Name).HasMaxLength(200);
        builder.Property(product => product.Slug)
            .HasConversion(slug => slug.Value, value => Slug.FromName(value).Value)
            .HasMaxLength(Slug.MaxLength);
        builder.HasIndex(product => product.Slug).IsUnique();
        builder.Property(product => product.Description).HasMaxLength(4000);
        builder.Property(product => product.Attributes).IsJsonAttributes();
        builder.HasMany(product => product.Skus).WithOne().HasForeignKey("ProductId").IsRequired();
        builder.Property<uint>(CatalogDbContext.Version).IsRowVersion();
        builder.Property<DateTimeOffset>(AuditInterceptor.CreatedAt);
        builder.Property<DateTimeOffset>(AuditInterceptor.UpdatedAt);
    }
}
