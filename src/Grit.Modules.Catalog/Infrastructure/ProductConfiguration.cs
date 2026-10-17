using System.Text.Json;
using Grit.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
        // jsonb on the product row, so an image change moves the row version (xmin) like any other product write.
        builder.Property(product => product.Images)
            .HasColumnType("jsonb")
            .HasConversion(
                images => JsonSerializer.Serialize(images, JsonSerializerOptions.Web),
                json => JsonSerializer.Deserialize<List<ProductImage>>(json, JsonSerializerOptions.Web)!,
                new ValueComparer<IReadOnlyList<ProductImage>>(
                    (left, right) => left!.SequenceEqual(right!),
                    images => images.Aggregate(0, (hash, image) => HashCode.Combine(hash, image)),
                    images => images.ToList()));
        builder.HasMany(product => product.Skus).WithOne().HasForeignKey("ProductId").IsRequired();
        builder.Property<uint>(CatalogDbContext.Version).IsRowVersion();
        builder.Property<DateTimeOffset>(AuditInterceptor.CreatedAt);
        builder.Property<DateTimeOffset>(AuditInterceptor.UpdatedAt);
        builder.Property<Guid?>(AuditInterceptor.CreatedBy);
        builder.Property<Guid?>(AuditInterceptor.UpdatedBy);
    }
}
