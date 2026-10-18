using System.Net;

namespace Grit.IntegrationTests;

public class ProductSearchValidationTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("q=t%00", "q")]
    [InlineData("category=camping%00", "category")]
    [InlineData("minPrice=-0.01", "minPrice")]
    [InlineData("maxPrice=-0.01", "maxPrice")]
    [InlineData("minPrice=10&maxPrice=9.99", "maxPrice")]
    [InlineData("attribute=cor", "attribute")]
    [InlineData("attribute=cor:", "attribute")]
    [InlineData("attribute=cor:azul%00", "attribute")]
    [InlineData("attribute=cor:azul&attribute=cor:preto", "attribute")]
    [InlineData("attribute=Cor:azul", "attribute")]
    [InlineData("after=garbage", "after")]
    [InlineData("after=-1_0192f0c1d2e37b4c8a9f000000000001", "after")]
    [InlineData("after=79228162514264337593543950336_0192f0c1d2e37b4c8a9f000000000001", "after")]
    [InlineData("after=0.5_not-a-uuid", "after")]
    [InlineData("after=0.5_0192f0c1d2e37b4c8a9f000000000001_2", "after")]
    [InlineData("after=0%00.5_0192f0c1d2e37b4c8a9f000000000001", "after")]
    [InlineData("sort=cheapest", "sort")]
    [InlineData("sort=PRICE_ASC", "sort")]
    [InlineData("limit=0", "limit")]
    [InlineData("limit=51", "limit")]
    [InlineData("limit=2147483647", "limit")]
    public async Task Search_InvalidParameter_Returns400WithAFieldError(string queryString, string field)
    {
        var response = await _client.GetAsync($"/api/v1/catalog/products?{queryString}", TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
        problem.GetProperty("errors").EnumerateObject().Select(error => error.Name).ShouldBe([field]);
    }

    [Fact]
    public async Task Search_QueryAbove100Characters_Returns400()
    {
        var response = await _client.GetAsync($"/api/v1/catalog/products?q={new string('a', 101)}", TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.GetProperty("code").GetString().ShouldBe("validation.failed");
        problem.GetProperty("errors").EnumerateObject().Select(error => error.Name).ShouldBe(["q"]);
    }

    // Binding fails before validation, so the framework answers without a code (ADR 0006).
    [Theory]
    [InlineData("minPrice=abc")]
    [InlineData("maxPrice=79228162514264337593543950336")]
    [InlineData("limit=2147483648")]
    public async Task Search_ParameterThatDoesNotBind_Returns400WithoutCode(string queryString)
    {
        var response = await _client.GetAsync($"/api/v1/catalog/products?{queryString}", TestContext.Current.CancellationToken);

        var problem = await ProblemResponse.ReadAsync(response, HttpStatusCode.BadRequest);
        problem.TryGetProperty("code", out _).ShouldBeFalse();
    }
}
