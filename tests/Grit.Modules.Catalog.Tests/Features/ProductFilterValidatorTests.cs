using Grit.Modules.Catalog.Features;

namespace Grit.Modules.Catalog.Tests.Features;

public class ProductFilterValidatorTests
{
    private static readonly ProductFilterValidator Validator = new();

    [Fact]
    public void Validate_EmptyFilter_IsValid()
    {
        Validator.Validate(new Filter()).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_EveryFieldAtItsLimit_IsValid()
    {
        var filter = new Filter(
            Query: new string('q', 100),
            Category: new string('c', 100),
            MinPrice: 0m,
            MaxPrice: 0m,
            Attributes: ["cor:azul", "tamanho:42"]);

        Validator.Validate(filter).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_OnlyMaxPrice_IsValid()
    {
        Validator.Validate(new Filter(MaxPrice: 5m)).IsValid.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(InvalidFilters))]
    public void Validate_InvalidField_FailsOnThatQueryParameter(Filter filter, string parameter)
    {
        var result = Validator.Validate(filter);

        result.Errors.Select(error => error.PropertyName).Distinct().ShouldBe([parameter]);
    }

    public static TheoryData<Filter, string> InvalidFilters => new()
    {
        { new Filter(Query: new string('q', 101)), "q" },
        { new Filter(Query: "tênis\u0000"), "q" },
        { new Filter(Category: new string('c', 101)), "Category" },
        { new Filter(Category: "camping\u0000"), "Category" },
        { new Filter(MinPrice: -0.01m), "MinPrice" },
        { new Filter(MaxPrice: -0.01m), "MaxPrice" },
        { new Filter(MinPrice: 10m, MaxPrice: 9.99m), "MaxPrice" },
        { new Filter(Attributes: ["cor"]), "attribute" },
    };

    public sealed record Filter(
        string? Query = null,
        string? Category = null,
        decimal? MinPrice = null,
        decimal? MaxPrice = null,
        IReadOnlyList<string>? Attributes = null) : IProductFilter
    {
        IReadOnlyList<string> IProductFilter.Attributes => Attributes ?? [];
    }
}
