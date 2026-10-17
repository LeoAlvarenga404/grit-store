using Grit.Modules.Catalog.Features.SearchProducts;
using Grit.Modules.Catalog.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Grit.Modules.Catalog.Tests.Infrastructure;

public class CatalogCacheTests
{
    [Theory]
    [InlineData(" tenis ", "tenis")]
    [InlineData("TENIS", "tenis")]
    [InlineData("Tênis", "tenis")]
    [InlineData("  Chapéu VERDE ", "chapeu verde")]
    public void SuggestionsKeyOf_TextThatDiffersOnlyInSpacesCaseOrAccents_GivesTheSameKey(string text, string plain) =>
        CatalogCache.SuggestionsKeyOf(text).ShouldBe(CatalogCache.SuggestionsKeyOf(plain));

    [Theory]
    [InlineData("tenis", "tenis c")]
    [InlineData("tenis", "tens")]
    [InlineData("tenis-c", "tenis c")]
    public void SuggestionsKeyOf_DifferentText_GivesDifferentKeys(string text, string other) =>
        CatalogCache.SuggestionsKeyOf(text).ShouldNotBe(CatalogCache.SuggestionsKeyOf(other));

    [Fact]
    public void KeyOf_QueriesThatDiffer_GiveDifferentKeys()
    {
        var query = new SearchProductsQuery("tenis", null, null, null, [], ProductSort.Relevance, null, 20);

        CatalogCache.KeyOf("search", query).ShouldBe(CatalogCache.KeyOf("search", query with { }));
        CatalogCache.KeyOf("search", query).ShouldNotBe(CatalogCache.KeyOf("search", query with { Limit = 21 }));
        CatalogCache.KeyOf("search", query).ShouldNotBe(CatalogCache.KeyOf("search", query with { Attributes = ["cor:azul"] }));
        CatalogCache.KeyOf("search", query).ShouldNotBe(CatalogCache.KeyOf("search", query with { Sort = ProductSort.PriceAscending }));
        CatalogCache.KeyOf("search", query).ShouldNotBe(CatalogCache.KeyOf("facets", query));
    }

    [Fact]
    public void IsSuccess_SuccessAndProblemResults_AreToldApart()
    {
        Results<Created<string>, ProblemHttpResult> created = TypedResults.Created((string?)null, "criado");
        Results<Ok<string>, ProblemHttpResult> conflict = TypedResults.Problem(statusCode: StatusCodes.Status409Conflict);

        CatalogCache.IsSuccess(created).ShouldBeTrue();
        CatalogCache.IsSuccess(TypedResults.Ok()).ShouldBeTrue();
        CatalogCache.IsSuccess(conflict).ShouldBeFalse();
        CatalogCache.IsSuccess(TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest)).ShouldBeFalse();
    }
}
