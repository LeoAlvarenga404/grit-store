using Grit.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grit.Modules.Identity.Infrastructure;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    // Get-only properties are mapped only when named here; the constructor then binds them.
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).ValueGeneratedNever();
        builder.Property(token => token.UserId);
        builder.Property(token => token.FamilyId);
        builder.Property(token => token.TokenHash);
        builder.Property(token => token.CreatedAt);
        builder.Property(token => token.ExpiresAt);
        builder.Property(token => token.ReplacedAt);
        builder.Property(token => token.RevokedAt);
        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => token.FamilyId);
        builder.HasOne<User>().WithMany().HasForeignKey(token => token.UserId);
    }
}
