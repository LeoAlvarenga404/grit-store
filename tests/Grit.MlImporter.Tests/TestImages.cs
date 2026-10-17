using SkiaSharp;

namespace Grit.MlImporter.Tests;

internal static class TestImages
{
    public static byte[] Jpeg(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.OrangeRed);
        using var image = SKImage.FromBitmap(bitmap);
        using var jpeg = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        return jpeg.ToArray();
    }

    public static (SKEncodedImageFormat Format, int Width, int Height) Describe(byte[] bytes)
    {
        using var codec = SKCodec.Create(new SKMemoryStream(bytes));
        return (codec.EncodedFormat, codec.Info.Width, codec.Info.Height);
    }
}
