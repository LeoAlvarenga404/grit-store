using System.Globalization;
using Bogus;
using Grit.Modules.Catalog.Domain;
using Grit.SharedKernel;

namespace Grit.Modules.Catalog.Infrastructure.Seed;

// Product i always comes out the same (seed i, model number i + 1 in the name, so a unique slug): a restart finds by
// slug what an earlier run wrote. Not thread-safe: one instance per seeding run.
internal sealed class VolumeCatalog(IReadOnlyDictionary<string, CategoryId> categoryIds)
{
    private static readonly string[] ShoeSizes = ["37", "38", "39", "40", "41", "42", "43", "44"];

    private static readonly string[] ClothingSizes = ["P", "M", "G", "GG"];

    private static readonly string[] OneSize = ["único"];

    private static readonly string[] Colors = ["preto", "azul", "verde", "vermelho", "cinza", "laranja"];

    private static readonly (string Category, string Name, string[] Sizes)[] Types =
    [
        ("Calçados", "Tênis de Corrida", ShoeSizes),
        ("Calçados", "Tênis de Trilha", ShoeSizes),
        ("Calçados", "Bota de Caminhada", ShoeSizes),
        ("Calçados", "Sandália Esportiva", ShoeSizes),
        ("Vestuário", "Camiseta Dry Fit", ClothingSizes),
        ("Vestuário", "Jaqueta Corta-Vento", ClothingSizes),
        ("Vestuário", "Bermuda de Corrida", ClothingSizes),
        ("Vestuário", "Calça de Trilha", ClothingSizes),
        ("Camping", "Barraca", OneSize),
        ("Camping", "Saco de Dormir", OneSize),
        ("Camping", "Mochila Cargueira", OneSize),
        ("Camping", "Lanterna de Cabeça", OneSize),
        ("Ciclismo", "Capacete de Ciclismo", ClothingSizes),
        ("Ciclismo", "Luva de Ciclismo", ClothingSizes),
        ("Ciclismo", "Bermuda de Ciclismo", ClothingSizes),
        ("Ciclismo", "Garrafa Térmica", OneSize),
    ];

    private readonly Faker _faker = new("pt_BR");

    // The categories of seed/catalog.json, so the generated products mix with the hand-written ones.
    public static IReadOnlyList<string> CategoryNames { get; } = [.. Types.Select(type => type.Category).Distinct()];

    // Re-seeding one Faker repeats exactly what a new Faker per product would give, sixty times faster.
    public Product Generate(int index)
    {
        _faker.Random = new Randomizer(index);
        var type = _faker.PickRandom(Types);
        var brand = _faker.Company.CompanyName();
        var material = _faker.Commerce.ProductMaterial();
        var model = (index + 1).ToString("D5", CultureInfo.InvariantCulture);
        var name = $"{type.Name} {_faker.Commerce.ProductAdjective()} {model}";
        var description = $"{type.Name} da {brand} em {material.ToLowerInvariant()}, para treinos e trilhas ao ar livre.";
        var attributes = new Dictionary<string, string> { ["marca"] = brand, ["material"] = material };
        var product = Product.Create(categoryIds[type.Category], name, description, attributes).Value;
        foreach (var color in _faker.PickRandom(Colors, _faker.Random.Int(1, 3)))
        {
            var price = Money.Create(Math.Round(_faker.Random.Decimal(39.90m, 1999.90m), 2), Product.StoreCurrency).Value;
            _ = product.AddSku(new Dictionary<string, string> { ["cor"] = color, ["tamanho"] = _faker.PickRandom(type.Sizes) }, price).Value;
        }

        return product;
    }
}
