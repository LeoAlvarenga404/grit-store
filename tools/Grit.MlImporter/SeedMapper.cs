using Grit.Modules.Catalog.Domain;
using Grit.Modules.Catalog.Infrastructure.Seed;
using Grit.SharedKernel;

namespace Grit.MlImporter;

// The limits are the API's (CreateProductValidator, AttributesRule, ADR 0009): the seeder goes through the domain only,
// so a longer name or a bad attribute key would reach the database and fail the whole seed.
internal static class SeedMapper
{
    public const int MaxNameLength = 200;

    public const int MaxDescriptionLength = 4000;

    public const int MaxAttributes = 20;

    public const int MaxAttributeKeyLength = 50;

    public const int MaxAttributeValueLength = 100;

    public static readonly Error NoPrice = new("importer.no_price", "No listing sells the product in BRL.", ErrorType.Validation);

    public static readonly Error NoName = new("importer.no_name", "The product name has no letter or digit.", ErrorType.Validation);

    private const string ColorId = "COLOR";

    private const string SizeId = "SIZE";

    private const string GenderKey = "genero";

    public static Result<SeedProduct> Map(MlProduct product, IReadOnlyList<MlItem> items, IReadOnlyList<string> imageFileNames)
    {
        var price = LowestPriceInBrl(items);
        if (price is null)
        {
            return NoPrice;
        }

        var name = Clean(product.Name, MaxNameLength);
        if (Slug.FromName(name).IsFailure)
        {
            return NoName;
        }

        var description = Clean(product.ShortDescription?.Content, MaxDescriptionLength);
        var attributes = product.Attributes ?? [];
        var sku = new SeedSku(SkuAttributes(attributes), price.Value);
        IReadOnlyList<SeedColorImages> images = imageFileNames.Count == 0 ? [] : [new SeedColorImages(sku.Attributes.GetValueOrDefault("cor"), imageFileNames)];
        return new SeedProduct(name, description.Length > 0 ? description : name, ProductAttributes(attributes), [sku], images);
    }

    // The cheapest listing is the price a shopper would find first; two decimals, as Money requires (ADR 0001).
    public static decimal? LowestPriceInBrl(IReadOnlyList<MlItem> items)
    {
        var prices = items.Where(item => item.CurrencyId == Product.StoreCurrency && item.Price > 0).Select(item => item.Price!.Value).ToList();
        return prices.Count == 0 ? null : decimal.Round(prices.Min(), 2, MidpointRounding.AwayFromZero);
    }

    // The winning listing is the price the product page shows; anything but a positive BRL price is no offer.
    public static decimal? PriceOf(MlBuyBoxWinner? winner) =>
        winner is { CurrencyId: Product.StoreCurrency, Price: > 0 } ? decimal.Round(winner.Price.Value, 2, MidpointRounding.AwayFromZero) : null;

    // Color and size become the SKU's variant; GTIN and package sizes say nothing to a shopper.
    public static Dictionary<string, string> ProductAttributes(IReadOnlyList<MlAttribute> attributes)
    {
        var result = new Dictionary<string, string>();
        foreach (var attribute in attributes)
        {
            if (result.Count == MaxAttributes)
            {
                break;
            }

            if (attribute.Id is ColorId or SizeId or "GTIN" || attribute.Id.StartsWith("PACKAGE_", StringComparison.Ordinal))
            {
                continue;
            }

            var key = AttributeKey(attribute.Name);
            var value = StoreValue(key, Clean(attribute.ValueName, MaxAttributeValueLength));
            if (key is not null && value.Length > 0)
            {
                result.TryAdd(key, value);
            }
        }

        return result;
    }

    public static Dictionary<string, string> SkuAttributes(IReadOnlyList<MlAttribute> attributes)
    {
        var result = new Dictionary<string, string>();
        var color = Clean(attributes.FirstOrDefault(attribute => attribute.Id == ColorId)?.ValueName, MaxAttributeValueLength);
        if (color.Length > 0)
        {
            result[Sku.ColorAttribute] = color.ToLowerInvariant();
        }

        var size = Clean(attributes.FirstOrDefault(attribute => attribute.Id == SizeId)?.ValueName, MaxAttributeValueLength);
        if (size.Length > 0)
        {
            result[Sku.SizeAttribute] = size;
        }

        return result;
    }

    // "Material da sola" -> "material-da-sola": the slug rule already gives lowercase ASCII letters, digits and '-'.
    public static string? AttributeKey(string name)
    {
        var slug = Slug.FromName(name);
        if (slug.IsFailure)
        {
            return null;
        }

        var key = slug.Value.Value;
        return key.Length <= MaxAttributeKeyLength ? key : key[..MaxAttributeKeyLength].TrimEnd('-');
    }

    // PostgreSQL rejects U+0000 and slugs throw on U+FFFE (TextRule); a cut never splits a surrogate pair.
    public static string Clean(string? text, int maxLength)
    {
        var cleaned = (text ?? "").Replace("\0", "", StringComparison.Ordinal).Replace("￾", "", StringComparison.Ordinal).Trim();
        if (cleaned.Length <= maxLength)
        {
            return cleaned;
        }

        var length = char.IsHighSurrogate(cleaned[maxLength - 1]) ? maxLength - 1 : maxLength;
        return cleaned[..length].TrimEnd();
    }

    // The store's gender facet is Masculino, Feminino or Unissex; the catalog says "Sem gênero" for the last.
    private static string StoreValue(string? key, string value) => key == GenderKey && value == "Sem gênero" ? "Unissex" : value;
}
