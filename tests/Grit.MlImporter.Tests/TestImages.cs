using SkiaSharp;

namespace Grit.MlImporter.Tests;

internal static class TestImages
{
    public static byte[] Jpeg(int width, int height) => Jpeg(width, height, SKColors.OrangeRed);

    public static byte[] Jpeg(int width, int height, SKColor color)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(color);
        using var image = SKImage.FromBitmap(bitmap);
        using var jpeg = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        return jpeg.ToArray();
    }

    public static (SKEncodedImageFormat Format, int Width, int Height) Describe(byte[] bytes)
    {
        using var codec = SKCodec.Create(new SKMemoryStream(bytes));
        return (codec.EncodedFormat, codec.Info.Width, codec.Info.Height);
    }

    public static SKColor CenterColor(byte[] bytes)
    {
        using var bitmap = SKBitmap.Decode(bytes);
        return bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2);
    }

    // JPEG then lossy WebP shift a flat colour by a few units; the markers differ by far more.
    public static bool IsNear(SKColor actual, SKColor expected) =>
        Math.Abs(actual.Red - expected.Red) <= 24 && Math.Abs(actual.Green - expected.Green) <= 24 && Math.Abs(actual.Blue - expected.Blue) <= 24;
}
