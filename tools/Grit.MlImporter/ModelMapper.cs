using Grit.Modules.Catalog.Domain;
using Grit.Modules.Catalog.Infrastructure.Seed;
using Grit.SharedKernel;

namespace Grit.MlImporter;

// One model (the priced leaves of a family) becomes one store product. Only what the catalog shows is used: a leaf
// without a colour is left out, a colour without a leaf is never created, and sizes are only those some leaf has.
internal static class ModelMapper
{
    public const int MinColors = 2;

    public static readonly Error TooFewColors = new("importer.too_few_colors", "The model has fewer than 2 colours with a listing.", ErrorType.Validation);

    public static string? ColorOf(MlProduct leaf) => SeedMapper.SkuAttributes(leaf.Attributes ?? []).GetValueOrDefault(Sku.ColorAttribute);

    public static IReadOnlyList<string> ColorsOf(IReadOnlyList<PricedLeaf> leaves) =>
        [.. leaves.Select(leaf => ColorOf(leaf.Product)).OfType<string>().Distinct()];

    // Leaves of one colour share most pictures, so each URL is kept once, in the order first seen.
    public static IReadOnlyList<ColorPictures> PicturesByColor(IReadOnlyList<PricedLeaf> leaves, int maxPerColor) =>
        [.. ColorsOf(leaves).Select(color => new ColorPictures(
            color,
            [.. leaves
                .Where(leaf => ColorOf(leaf.Product) == color)
                .SelectMany(leaf => leaf.Product.Pictures ?? [])
                .Select(picture => picture.Url)
                .Distinct()
                .Take(maxPerColor)]))];

    public static Result<SeedProduct> Map(IReadOnlyList<PricedLeaf> leaves, IReadOnlyList<SeedColorImages> images)
    {
        var colored = leaves.Where(leaf => ColorOf(leaf.Product) is not null).ToList();
        if (ColorsOf(colored).Count < MinColors)
        {
            return TooFewColors;
        }

        var first = colored[0].Product;
        var name = SeedMapper.Clean(string.IsNullOrWhiteSpace(first.FamilyName) ? first.Name : first.FamilyName, SeedMapper.MaxNameLength);
        if (Slug.FromName(name).IsFailure)
        {
            return SeedMapper.NoName;
        }

        var description = colored
            .Select(leaf => SeedMapper.Clean(leaf.Product.ShortDescription?.Content, SeedMapper.MaxDescriptionLength))
            .FirstOrDefault(text => text.Length > 0) ?? name;
        return new SeedProduct(name, description, CommonAttributes(colored), Variants(colored), images);
    }

    // Two leaves with the same colour and size are one variant listed twice: the cheaper price wins, in the order first seen.
    private static List<SeedSku> Variants(List<PricedLeaf> leaves) =>
        [.. leaves
            .Select(leaf => (Attributes: SeedMapper.SkuAttributes(leaf.Product.Attributes ?? []), leaf.Price))
            .GroupBy(leaf => (Color: leaf.Attributes.GetValueOrDefault(Sku.ColorAttribute), Size: leaf.Attributes.GetValueOrDefault(Sku.SizeAttribute)))
            .Select(variant => new SeedSku(variant.First().Attributes, variant.Min(leaf => leaf.Price)))];

    // A leaf also carries what belongs to its variant (main colour, size grid row, GTIN): only what every leaf shares
    // describes the model.
    private static Dictionary<string, string> CommonAttributes(List<PricedLeaf> leaves)
    {
        var common = SeedMapper.ProductAttributes(leaves[0].Product.Attributes ?? []);
        foreach (var leaf in leaves.Skip(1))
        {
            var attributes = SeedMapper.ProductAttributes(leaf.Product.Attributes ?? []);
            foreach (var key in common.Keys.ToList())
            {
                if (!attributes.TryGetValue(key, out var value) || value != common[key])
                {
                    common.Remove(key);
                }
            }
        }

        return common;
    }
}
