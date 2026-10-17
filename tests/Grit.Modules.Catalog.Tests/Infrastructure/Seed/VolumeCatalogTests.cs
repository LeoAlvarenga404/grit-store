using Grit.Modules.Catalog.Domain;
using Grit.Modules.Catalog.Infrastructure.Seed;

namespace Grit.Modules.Catalog.Tests.Infrastructure.Seed;

public class VolumeCatalogTests
{
    private static readonly Dictionary<string, CategoryId> CategoryIds =
        VolumeCatalog.CategoryNames.ToDictionary(name => name, _ => CategoryId.New());

    [Fact]
    public void Generate_SameIndexTwice_GivesTheSameProduct()
    {
        var first = new VolumeCatalog(CategoryIds).Generate(41);
        var catalog = new VolumeCatalog(CategoryIds);
        _ = catalog.Generate(7);

        var second = catalog.Generate(41);

        second.Name.ShouldBe(first.Name);
        second.Description.ShouldBe(first.Description);
        second.Attributes.ShouldBe(first.Attributes);
        second.Skus.Select(sku => $"{sku.Attributes["cor"]}/{sku.Attributes["tamanho"]}/{sku.Price.Amount}")
            .ShouldBe(first.Skus.Select(sku => $"{sku.Attributes["cor"]}/{sku.Attributes["tamanho"]}/{sku.Price.Amount}"));
    }

    [Fact]
    public void Generate_TwoThousandIndexes_GivesUniqueSlugsAndOneToThreeSkus()
    {
        var catalog = new VolumeCatalog(CategoryIds);

        var products = Enumerable.Range(0, 2000).Select(catalog.Generate).ToList();

        products.Select(product => product.Slug.Value).Distinct().Count().ShouldBe(2000);
        products.ShouldAllBe(product => product.Skus.Count >= 1 && product.Skus.Count <= 3);
    }

    [Fact]
    public void Generate_Product_BelongsToTheCategoryOfItsType()
    {
        var catalog = new VolumeCatalog(CategoryIds);

        var products = Enumerable.Range(0, 200).Select(catalog.Generate).ToList();

        products.Where(product => product.Name.StartsWith("Tênis", StringComparison.Ordinal))
            .ShouldAllBe(product => product.CategoryId == CategoryIds["Calçados"]);
        products.Select(product => product.CategoryId).Distinct().Count().ShouldBe(4);
    }
}
