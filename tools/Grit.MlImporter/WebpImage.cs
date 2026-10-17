using SkiaSharp;

namespace Grit.MlImporter;

// Mercado Livre serves pictures up to 1200 px; 800 px WebP keeps the committed seed at a few MB.
internal static class WebpImage
{
    public const int MaxSideInPixels = 800;

    private const int Quality = 80;

    // Null when the bytes are not an image SkiaSharp can decode (an HTML error page, a truncated download).
    public static byte[]? Convert(byte[] bytes)
    {
        using var stream = new SKMemoryStream(bytes);
        using var codec = SKCodec.Create(stream);
        using var source = codec is null ? null : SKBitmap.Decode(codec);
        if (source is null)
        {
            return null;
        }

        var scale = Math.Min(1.0, (double)MaxSideInPixels / Math.Max(source.Width, source.Height));
        var size = new SKImageInfo(Scaled(source.Width, scale), Scaled(source.Height, scale));
        using var resized = source.Resize(size, new SKSamplingOptions(SKCubicResampler.Mitchell))
            ?? throw new InvalidOperationException($"SkiaSharp could not resize a {source.Width}x{source.Height} image.");
        using var image = SKImage.FromBitmap(resized);
        using var webp = image.Encode(SKEncodedImageFormat.Webp, Quality);
        return webp.ToArray();
    }

    private static int Scaled(int side, double scale) => Math.Max(1, (int)Math.Round(side * scale));
}
