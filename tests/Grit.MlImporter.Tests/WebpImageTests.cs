using System.Text;
using SkiaSharp;

namespace Grit.MlImporter.Tests;

public class WebpImageTests
{
    [Theory]
    [InlineData(1600, 1000, 800, 500)]
    [InlineData(1000, 1600, 500, 800)]
    [InlineData(300, 200, 300, 200)]
    public void Convert_Jpeg_GivesWebpWithTheLongestSideAtMost800(int width, int height, int expectedWidth, int expectedHeight)
    {
        var webp = WebpImage.Convert(TestImages.Jpeg(width, height));

        webp.ShouldNotBeNull();
        TestImages.Describe(webp).ShouldBe((SKEncodedImageFormat.Webp, expectedWidth, expectedHeight));
    }

    [Fact]
    public void Convert_HtmlInsteadOfAnImage_ReturnsNull()
    {
        WebpImage.Convert(Encoding.UTF8.GetBytes("<html>Erro 503</html>")).ShouldBeNull();
    }
}
