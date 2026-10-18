using System.Globalization;
using Grit.Modules.Catalog.Domain;
using Grit.SharedKernel;

namespace Grit.MlImporter;

internal sealed record ImportOptions(IReadOnlyList<ImportCategory> Categories, int ProductsPerCategory, string OutputDirectory)
{
    public const int DefaultProductsPerCategory = 10;

    // Each product brings up to 3 photos per colour (CatalogImporter.MaxImagesPerColor): the cap keeps the committed seed small.
    public const int MaxProductsPerCategory = 20;

    public const string Usage = """
        Usage: dotnet run --project tools/Grit.MlImporter -- --category "<store category>=<search terms>[@<MLB domain id>]" [--category ...] [--count 10] [--output seed]
          --category  repeatable; the store category (existing like "Tênis" or new) and the Mercado Livre search terms
                      the optional domain (e.g. MLB-SNEAKERS) keeps the search inside one Mercado Livre domain
                      repeat a store category to search it with other terms or domains, in the order given
          --count     models (store products) per category, 1 to 20 (default 10)
          --output    folder that receives catalog.json and images/ (default seed)
        The access token comes from: dotnet user-secrets set "MercadoLivre:AccessToken" "<token>" --project tools/Grit.MlImporter
        """;

    public static Result<ImportOptions> Parse(IReadOnlyList<string> args)
    {
        var categories = new List<ImportCategory>();
        var count = DefaultProductsPerCategory;
        var output = "seed";
        for (var index = 0; index < args.Count; index += 2)
        {
            var value = index + 1 < args.Count ? args[index + 1] : null;
            if (value is null)
            {
                return Invalid($"'{args[index]}' needs a value.");
            }

            switch (args[index])
            {
                case "--category" when ImportCategory.Parse(value) is { } category:
                    categories.Add(category);
                    break;
                case "--count" when int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed is > 0 and <= MaxProductsPerCategory:
                    count = parsed;
                    break;
                case "--output" when value.Length > 0:
                    output = value;
                    break;
                default:
                    return Invalid($"'{args[index]} {value}' is not a valid option.");
            }
        }

        return Check(new ImportOptions(categories, count, output));
    }

    // The same name repeated is one category with several searches; two spellings of one slug would be one category
    // for the seeder under two names, which it rejects.
    private static Result<ImportOptions> Check(ImportOptions options)
    {
        if (options.Categories.Count == 0)
        {
            return Invalid("At least one --category is required.");
        }

        var slugs = options.Categories.Select(category => category.StoreName).Distinct().Select(name => Slug.FromName(name).Value).ToList();
        return slugs.Distinct().Count() == slugs.Count ? options : Invalid("Two --category options spell the same store category differently.");
    }

    private static Error Invalid(string message) => new("importer.invalid_arguments", message, ErrorType.Validation);
}
