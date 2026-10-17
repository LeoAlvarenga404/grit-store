using Grit.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Grit.Modules.Ordering.Infrastructure;

internal static class MoneyMapping
{
    public static void Configure(ComplexPropertyBuilder<Money> money)
    {
        money.Property(value => value.Amount).HasPrecision(15, 2);
        money.Property(value => value.Currency).HasMaxLength(3).IsFixedLength();
    }
}
