namespace Grit.Modules.Catalog.Domain;

// The colours a product comes in, each with its image keys. Colours with images come first, in the order of their first
// image, so the first colour shows the cover; colours without images follow by name. A SKU without "cor" has no colour:
// its images stay only in the product's full gallery.
internal static class ProductColors
{
    public static IReadOnlyList<ProductColor> Group(IEnumerable<string?> skuColors, IReadOnlyList<ProductImage> images)
    {
        var names = skuColors.OfType<string>().ToHashSet(StringComparer.Ordinal);
        var withImages = images.Select(image => image.Color).OfType<string>().Where(names.Contains).Distinct(StringComparer.Ordinal).ToList();
        var withoutImages = names.Except(withImages, StringComparer.Ordinal).Order(StringComparer.Ordinal);
        return [.. withImages.Concat(withoutImages).Select(name => new ProductColor(name, [.. images.Where(image => image.Color == name).Select(image => image.Key)]))];
    }
}
