using Grit.SharedKernel;

namespace Grit.Modules.Catalog.Domain;

internal sealed class Category : AggregateRoot<CategoryId>
{
    private Category(CategoryId id, string name, Slug slug) : base(id)
    {
        Name = name;
        Slug = slug;
    }

    public string Name { get; }

    public Slug Slug { get; }

    public static Result<Category> Create(string name)
    {
        var slug = Slug.FromName(name);
        if (slug.IsFailure)
        {
            return slug.Error;
        }

        return new Category(CategoryId.New(), name, slug.Value);
    }
}
