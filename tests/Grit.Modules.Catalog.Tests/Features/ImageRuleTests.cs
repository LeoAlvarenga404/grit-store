using Grit.Modules.Catalog.Features;

namespace Grit.Modules.Catalog.Tests.Features;

public class ImageRuleTests
{
    [Theory]
    [InlineData("image/png", 2_097_152L, true)]
    [InlineData("image/webp", 1L, true)]
    [InlineData("image/png", 2_097_153L, false)]
    [InlineData("image/jpeg", 0L, false)]
    [InlineData("image/gif", 100L, false)]
    [InlineData(null, 100L, false)]
    public void IsAllowed_TypeAndSize_AllowsOnlyJpegPngWebpUpToTwoMegabytes(string? contentType, long sizeInBytes, bool expected)
    {
        ImageRule.IsAllowed(contentType, sizeInBytes).ShouldBe(expected);
    }
}
