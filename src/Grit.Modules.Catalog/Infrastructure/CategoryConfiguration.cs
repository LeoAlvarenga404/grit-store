using Grit.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grit.Modules.Catalog.Infrastructure;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Id)
            .HasConversion(id => id.Value, value => new CategoryId(value))
            .ValueGeneratedNever();
        builder.Property(category => category.Name).HasMaxLength(100);
        builder.Property(category => category.Slug)
            .HasConversion(slug => slug.Value, value => Slug.FromName(value).Value)
            .HasMaxLength(Slug.MaxLength);
        builder.HasIndex(category => category.Slug).IsUnique();
        builder.Property<DateTimeOffset>(AuditInterceptor.CreatedAt);
        builder.Property<DateTimeOffset>(AuditInterceptor.UpdatedAt);
        builder.Property<Guid?>(AuditInterceptor.CreatedBy);
        builder.Property<Guid?>(AuditInterceptor.UpdatedBy);
    }
}
