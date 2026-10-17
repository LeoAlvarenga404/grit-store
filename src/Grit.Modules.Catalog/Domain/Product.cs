using Grit.SharedKernel;

namespace Grit.Modules.Catalog.Domain;

internal sealed class Product : AggregateRoot<ProductId>
{
    // v1 sells in reais only, so basket and order totals always add up (Money.Add rejects mixed currencies).
    public const string StoreCurrency = "BRL";

    public const int MaxImages = 8;

    private readonly List<Sku> _skus = [];

    private Product(
        ProductId id,
        CategoryId categoryId,
        string name,
        Slug slug,
        string description,
        IReadOnlyDictionary<string, string> attributes) : base(id)
    {
        CategoryId = categoryId;
        Name = name;
        Slug = slug;
        Description = description;
        Attributes = attributes;
    }

    public CategoryId CategoryId { get; private set; }

    public string Name { get; private set; }

    public Slug Slug { get; }

    public string Description { get; private set; }

    public IReadOnlyDictionary<string, string> Attributes { get; private set; }

    public IReadOnlyList<Sku> Skus => _skus;

    // Storage keys in display order; the first image is the cover.
    public IReadOnlyList<string> ImageKeys { get; private set; } = [];

    public static Result<Product> Create(
        CategoryId categoryId,
        string name,
        string description,
        IReadOnlyDictionary<string, string> attributes)
    {
        var slug = Slug.FromName(name);
        if (slug.IsFailure)
        {
            return slug.Error;
        }

        return new Product(ProductId.New(), categoryId, name, slug.Value, description, new Dictionary<string, string>(attributes));
    }

    // Confirmed uploads are copied under this prefix, so a key from another product is never attached here.
    public static string ImageKeyPrefix(ProductId id) => $"products/{id.Value}/";

    // The slug is kept on rename so links already published keep working.
    public void Update(CategoryId categoryId, string name, string description, IReadOnlyDictionary<string, string> attributes)
    {
        CategoryId = categoryId;
        Name = name;
        Description = description;
        Attributes = new Dictionary<string, string>(attributes);
    }

    public Result<Sku> AddSku(IReadOnlyDictionary<string, string> attributes, Money price)
    {
        ArgumentNullException.ThrowIfNull(attributes);
        ArgumentNullException.ThrowIfNull(price);

        if (price.Currency != StoreCurrency)
        {
            return CatalogErrors.UnsupportedCurrency;
        }

        if (_skus.Any(sku => sku.Attributes.Count == attributes.Count && !sku.Attributes.Except(attributes).Any()))
        {
            return CatalogErrors.DuplicateVariant;
        }

        var sku = Sku.Create(attributes, price);
        _skus.Add(sku);
        return sku;
    }

    public Result ChangeSkuPrice(SkuId skuId, Money price)
    {
        ArgumentNullException.ThrowIfNull(price);

        if (price.Currency != StoreCurrency)
        {
            return CatalogErrors.UnsupportedCurrency;
        }

        var sku = _skus.Find(sku => sku.Id == skuId);
        if (sku is null)
        {
            return CatalogErrors.SkuNotFound;
        }

        sku.ChangePrice(price);
        return Result.Success();
    }

    public Result AddImage(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var prefix = ImageKeyPrefix(Id);
        if (!key.StartsWith(prefix, StringComparison.Ordinal) || key.Length == prefix.Length)
        {
            return CatalogErrors.InvalidImageKey;
        }

        // A repeated confirm of the same upload changes nothing.
        if (ImageKeys.Contains(key))
        {
            return Result.Success();
        }

        if (ImageKeys.Count >= MaxImages)
        {
            return CatalogErrors.ImageLimit;
        }

        ImageKeys = [.. ImageKeys, key];
        return Result.Success();
    }

    public Result RemoveImage(string key)
    {
        if (!ImageKeys.Contains(key))
        {
            return CatalogErrors.ImageNotFound;
        }

        ImageKeys = [.. ImageKeys.Where(existing => existing != key)];
        return Result.Success();
    }
}
