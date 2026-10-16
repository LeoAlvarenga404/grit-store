using System.Globalization;
using Grit.Modules.Catalog.Features.SearchProducts;

namespace Grit.Modules.Catalog.Tests.Features.SearchProducts;

public class ProductCursorTests
{
    private static readonly Guid Id = Guid.Parse("0192f0c1-d2e3-7b4c-8a9f-000000000001");

    [Theory]
    [InlineData("0")]
    [InlineData("1.607930")]
    [InlineData("0.5")]
    public void Encode_ThenTryParse_ReturnsTheSameCursor(string rank)
    {
        var cursor = new ProductCursor(decimal.Parse(rank, CultureInfo.InvariantCulture), Id);

        ProductCursor.TryParse(cursor.Encode(), out var parsed).ShouldBeTrue();

        parsed.ShouldBe(cursor);
    }

    [Fact]
    public void Encode_PortugueseCulture_WritesAPointAndTheIdWithoutHyphens()
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
        try
        {
            new ProductCursor(1.5m, Id).Encode().ShouldBe("1.5_0192f0c1d2e37b4c8a9f000000000001");
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1.5")]
    [InlineData("1,5_0192f0c1d2e37b4c8a9f000000000001")]
    [InlineData("-1_0192f0c1d2e37b4c8a9f000000000001")]
    [InlineData("1.5_0192f0c1-d2e3-7b4c-8a9f-000000000001")]
    [InlineData("1.5_0192f0c1d2e37b4c8a9f000000000001_2")]
    [InlineData("abc_0192f0c1d2e37b4c8a9f000000000001")]
    public void TryParse_MalformedText_ReturnsFalse(string? text)
    {
        ProductCursor.TryParse(text, out var cursor).ShouldBeFalse();

        cursor.ShouldBeNull();
    }
}
