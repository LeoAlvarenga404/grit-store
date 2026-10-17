using Grit.SharedKernel;

namespace Grit.Modules.Catalog.Domain;

internal static class CatalogErrors
{
    public static readonly Error EmptySlug = new("catalog.empty_slug", "The name must contain at least one letter or digit.", ErrorType.Validation);

    public static readonly Error CategoryNotFound = new("catalog.category_not_found", "The category does not exist.", ErrorType.Validation);

    public static readonly Error CategorySlugTaken = new("catalog.category_slug_taken", "Another category already has a name with the same slug.", ErrorType.Conflict);

    public static readonly Error ProductNotFound = new("catalog.product_not_found", "The product does not exist.", ErrorType.NotFound);

    public static readonly Error ProductSlugTaken = new("catalog.product_slug_taken", "Another product already has a name with the same slug.", ErrorType.Conflict);

    public static readonly Error ProductChanged = new("catalog.product_changed", "The product was changed by someone else. Reload it and try again.", ErrorType.Conflict);

    public static readonly Error SkuNotFound = new("catalog.sku_not_found", "The product has no SKU with this id.", ErrorType.NotFound);

    public static readonly Error UnsupportedCurrency = new("catalog.unsupported_currency", "Prices must be in BRL.", ErrorType.Validation);

    public static readonly Error DuplicateVariant = new("catalog.duplicate_variant", "The product already has a SKU with these attributes.", ErrorType.Conflict);
}
