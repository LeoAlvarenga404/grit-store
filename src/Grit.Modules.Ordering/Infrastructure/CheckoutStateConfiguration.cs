using Grit.Modules.Ordering.Features.Checkout;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grit.Modules.Ordering.Infrastructure;

internal sealed class CheckoutStateConfiguration : IEntityTypeConfiguration<CheckoutState>
{
    public void Configure(EntityTypeBuilder<CheckoutState> builder)
    {
        builder.ToTable("checkout_states");
        builder.HasKey(state => state.CorrelationId);
        builder.Property(state => state.CorrelationId).ValueGeneratedNever();
        builder.Property(state => state.CurrentState).HasMaxLength(64);
        builder.Property(state => state.TotalAmount).HasPrecision(15, 2);
        builder.Property(state => state.Currency).HasMaxLength(3).IsFixedLength();
        builder.Property(state => state.PaymentToken).HasMaxLength(100);
        builder.Property(state => state.AuthorizationId).HasMaxLength(100);
    }
}
