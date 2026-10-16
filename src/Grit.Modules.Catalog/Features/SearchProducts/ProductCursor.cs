using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Grit.Modules.Catalog.Features.SearchProducts;

// Keyset position: the last item's rank and id. Invariant culture, so a pt-BR server writes "1.5", never "1,5".
internal sealed record ProductCursor(decimal Rank, Guid Id)
{
    public string Encode() => string.Create(CultureInfo.InvariantCulture, $"{Rank}_{Id:N}");

    public static bool TryParse(string? text, [NotNullWhen(true)] out ProductCursor? cursor)
    {
        cursor = null;
        var parts = text?.Split('_');
        if (parts is not [var rank, var id]
            || !decimal.TryParse(rank, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var rankValue)
            || !Guid.TryParseExact(id, "N", out var idValue))
        {
            return false;
        }

        cursor = new ProductCursor(rankValue, idValue);
        return true;
    }
}
