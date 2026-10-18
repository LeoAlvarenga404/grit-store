using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Grit.Modules.Catalog.Features.SearchProducts;

// Keyset position: the last item's sort value (its rank or its price) and id. Invariant culture, so a pt-BR server writes "1.5", never "1,5".
internal sealed record ProductCursor(decimal SortValue, Guid Id)
{
    public string Encode() => string.Create(CultureInfo.InvariantCulture, $"{SortValue}_{Id:N}");

    public static bool TryParse(string? text, [NotNullWhen(true)] out ProductCursor? cursor)
    {
        cursor = null;
        var parts = text?.Split('_');
        if (parts is not [var sortValue, var id]
            || !decimal.TryParse(sortValue, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var sortValueNumber)
            || !Guid.TryParseExact(id, "N", out var idValue))
        {
            return false;
        }

        cursor = new ProductCursor(sortValueNumber, idValue);
        return true;
    }
}
