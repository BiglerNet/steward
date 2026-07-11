using Steward.Application.Storage;
using Steward.Domain.Common.Exceptions;
using SkiaSharp;

namespace Steward.Infrastructure.Storage;

public class SkiaImageProcessor : IImageProcessor
{
    private const int MaxInputDimensionPx = 12_000;
    private const int DisplayMaxDimensionPx = 2048;
    private const int ThumbMaxDimensionPx = 320;
    private const int JpegQuality = 80;

    private static readonly SKEncodedImageFormat[] AcceptedFormats =
    [
        SKEncodedImageFormat.Jpeg,
        SKEncodedImageFormat.Png,
        SKEncodedImageFormat.Webp,
    ];

    public ProcessedPhoto Process(Stream input)
    {
        using var data = SKData.Create(input);
        if (data is null || data.Size == 0)
        {
            throw new BadRequestException("Uploaded file is not a supported image.");
        }

        using var codec = SKCodec.Create(data);
        if (codec is null)
        {
            throw new BadRequestException("Uploaded file is not a supported image.");
        }

        if (!AcceptedFormats.Contains(codec.EncodedFormat))
        {
            throw new BadRequestException("Unsupported image format. Only JPEG, PNG, and WebP are accepted.");
        }

        var info = codec.Info;
        if (info.Width > MaxInputDimensionPx || info.Height > MaxInputDimensionPx)
        {
            throw new BadRequestException("Image dimensions exceed the maximum allowed size.");
        }

        using var bitmap = SKBitmap.Decode(codec);
        if (bitmap is null)
        {
            throw new BadRequestException("Uploaded file is not a supported image.");
        }

        var oriented = ApplyOrientation(bitmap, codec.EncodedOrigin);
        try
        {
            var (displayBytes, displayWidth, displayHeight) = ResizeAndEncode(oriented, DisplayMaxDimensionPx);
            var (thumbBytes, _, _) = ResizeAndEncode(oriented, ThumbMaxDimensionPx);

            return new ProcessedPhoto(thumbBytes, displayBytes, displayWidth, displayHeight);
        }
        finally
        {
            if (!ReferenceEquals(oriented, bitmap))
            {
                oriented.Dispose();
            }
        }
    }

    private static SKBitmap ApplyOrientation(SKBitmap source, SKEncodedOrigin origin)
    {
        if (origin is SKEncodedOrigin.TopLeft or SKEncodedOrigin.Default)
        {
            return source;
        }

        var swapsDimensions = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

        var width = swapsDimensions ? source.Height : source.Width;
        var height = swapsDimensions ? source.Width : source.Height;

        var rotated = new SKBitmap(width, height, source.ColorType, source.AlphaType);
        using var canvas = new SKCanvas(rotated);

        switch (origin)
        {
            case SKEncodedOrigin.TopRight: // mirrored horizontally
                canvas.Translate(width, 0);
                canvas.Scale(-1, 1);
                break;
            case SKEncodedOrigin.BottomRight: // rotated 180
                canvas.Translate(width, height);
                canvas.RotateDegrees(180);
                break;
            case SKEncodedOrigin.BottomLeft: // mirrored vertically
                canvas.Translate(0, height);
                canvas.Scale(1, -1);
                break;
            case SKEncodedOrigin.LeftTop: // transposed
                canvas.RotateDegrees(90);
                canvas.Scale(1, -1);
                break;
            case SKEncodedOrigin.RightTop: // rotated 90 CW
                canvas.Translate(width, 0);
                canvas.RotateDegrees(90);
                break;
            case SKEncodedOrigin.RightBottom: // transversed
                canvas.Translate(width, height);
                canvas.RotateDegrees(-90);
                canvas.Scale(-1, 1);
                break;
            case SKEncodedOrigin.LeftBottom: // rotated 270 CW
                canvas.Translate(0, height);
                canvas.RotateDegrees(270);
                break;
        }

        using (var sourceImage = SKImage.FromBitmap(source))
        {
            canvas.DrawImage(sourceImage, 0, 0, SKSamplingOptions.Default);
        }

        return rotated;
    }

    private static (byte[] Bytes, int Width, int Height) ResizeAndEncode(SKBitmap source, int maxDimensionPx)
    {
        var longEdge = Math.Max(source.Width, source.Height);
        var target = source;
        var ownsTarget = false;

        if (longEdge > maxDimensionPx)
        {
            var scale = (double)maxDimensionPx / longEdge;
            var width = Math.Max(1, (int)Math.Round(source.Width * scale));
            var height = Math.Max(1, (int)Math.Round(source.Height * scale));

            var resized = source.Resize(
                new SKImageInfo(width, height, source.ColorType, source.AlphaType),
                new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));

            if (resized is null)
            {
                throw new BadRequestException("Failed to process image.");
            }

            target = resized;
            ownsTarget = true;
        }

        try
        {
            using var image = SKImage.FromBitmap(target);
            using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
            return (encoded.ToArray(), target.Width, target.Height);
        }
        finally
        {
            if (ownsTarget)
            {
                target.Dispose();
            }
        }
    }
}
