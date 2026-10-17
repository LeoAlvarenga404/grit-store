using System.Globalization;
using Grit.Modules.Catalog.Domain;
using Grit.SharedKernel;

namespace Grit.MlImporter;

internal sealed record ImportOptions(IReadOnlyList<ImportCategory> Categories, int ProductsPerCategory, string OutputDirectory)
{
    public const int DefaultProductsPerCategory = 5;

    // 20 products x 2 images x ~50 KB per category: the cap that keeps the committed seed small.
    public const int MaxProductsPerCategory = 20;

    public const string Usage = """
        Usage: dotnet run --project tools/Grit.MlImporter -- --category "<store category>=<search terms>" [--category ...] [--count 5] [--output seed]
          --category  repeatable; the store category (existing like "Camping" or new) and the Mercado Livre search terms
          --count     products per category, 1 to 20 (default 5)
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

    // Two names with one slug would be one category for the seeder, which rejects the file.
    private static Result<ImportOptions> Check(ImportOptions options)
    {
        if (options.Categories.Count == 0)
        {
            return Invalid("At least one --category is required.");
        }

        var slugs = options.Categories.Select(category => Slug.FromName(category.StoreName).Value).ToList();
        return slugs.Distinct().Count() == slugs.Count ? options : Invalid("Two --category options name the same store category.");
    }

    private static Error Invalid(string message) => new("importer.invalid_arguments", message, ErrorType.Validation);
}
