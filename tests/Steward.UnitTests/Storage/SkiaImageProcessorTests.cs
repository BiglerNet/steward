using Steward.Domain.Common.Exceptions;
using Steward.Infrastructure.Storage;
using SkiaSharp;

namespace Steward.UnitTests.Storage;

public class SkiaImageProcessorTests
{
    private readonly SkiaImageProcessor _processor = new();

    [Fact]
    public void Process_Applies_Exif_Orientation_And_Rotates_Landscape_To_Portrait()
    {
        // 100x60 landscape with a red marker in the raw top-left corner. Orientation 6
        // (RightTop) says the raw data must be rotated 90deg clockwise to be upright.
        var raw = CreateMarkedJpeg(width: 100, height: 60, markerSize: 10);
        var tagged = InjectExifOrientation(raw, orientation: 6);

        var result = _processor.Process(new MemoryStream(tagged));

        Assert.Equal(60, result.Width);
        Assert.Equal(100, result.Height);

        using var displayBitmap = SKBitmap.Decode(result.DisplayBytes);
        Assert.Equal(60, displayBitmap.Width);
        Assert.Equal(100, displayBitmap.Height);

        // Rotating 90deg CW moves the raw top-left marker to the top-right of the output.
        // JPEG re-encoding is lossy, so allow a tolerance rather than an exact color match.
        var topRight = displayBitmap.GetPixel(55, 5);
        var topLeft = displayBitmap.GetPixel(5, 5);
        Assert.True(topRight.Red > 200 && topRight.Green < 60 && topRight.Blue < 60, $"Expected red-ish, got {topRight}");
        Assert.True(topLeft.Red > 200 && topLeft.Green > 200 && topLeft.Blue > 200, $"Expected white-ish, got {topLeft}");
    }

    [Fact]
    public void Process_Strips_Exif_Metadata_From_Output()
    {
        var raw = CreateMarkedJpeg(width: 50, height: 50, markerSize: 5);
        var tagged = InjectExifOrientation(raw, orientation: 6);

        var result = _processor.Process(new MemoryStream(tagged));

        Assert.False(ContainsAscii(result.DisplayBytes, "Exif"));
        Assert.False(ContainsAscii(result.ThumbBytes, "Exif"));
    }

    [Fact]
    public void Process_Does_Not_Upscale_Small_Images()
    {
        var bytes = CreateMarkedJpeg(width: 100, height: 60, markerSize: 10);

        var result = _processor.Process(new MemoryStream(bytes));

        Assert.Equal(100, result.Width);
        Assert.Equal(60, result.Height);

        using var thumbBitmap = SKBitmap.Decode(result.ThumbBytes);
        Assert.Equal(100, thumbBitmap.Width);
        Assert.Equal(60, thumbBitmap.Height);
    }

    [Fact]
    public void Process_Downscales_Large_Images_Without_Exceeding_Caps()
    {
        var bytes = CreateMarkedJpeg(width: 3000, height: 1500, markerSize: 10);

        var result = _processor.Process(new MemoryStream(bytes));

        Assert.Equal(2048, result.Width);
        Assert.Equal(1024, result.Height);

        using var thumbBitmap = SKBitmap.Decode(result.ThumbBytes);
        Assert.Equal(320, thumbBitmap.Width);
        Assert.Equal(160, thumbBitmap.Height);
    }

    [Fact]
    public void Process_Rejects_Disguised_Non_Image()
    {
        var bytes = "this is definitely not an image"u8.ToArray();

        Assert.Throws<BadRequestException>(() => _processor.Process(new MemoryStream(bytes)));
    }

    [Fact]
    public void Process_Rejects_Oversized_Dimensions_Before_Full_Decode()
    {
        var jpeg = CreateMarkedJpeg(width: 8, height: 8, markerSize: 2);
        var tampered = PatchSof0Dimensions(jpeg, width: 20_000, height: 20_000);

        Assert.Throws<BadRequestException>(() => _processor.Process(new MemoryStream(tampered)));
    }

    private static bool ContainsAscii(byte[] haystack, string needle)
    {
        var needleBytes = System.Text.Encoding.ASCII.GetBytes(needle);
        for (var i = 0; i <= haystack.Length - needleBytes.Length; i++)
        {
            var match = true;
            for (var j = 0; j < needleBytes.Length; j++)
            {
                if (haystack[i + j] != needleBytes[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return true;
            }
        }

        return false;
    }

    private static byte[] CreateMarkedJpeg(int width, int height, int markerSize)
    {
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            using var paint = new SKPaint { Color = SKColors.Red };
            canvas.DrawRect(new SKRect(0, 0, markerSize, markerSize), paint);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, 100);
        return encoded.ToArray();
    }

    /// Splices a minimal EXIF APP1 segment (single Orientation tag, little-endian TIFF) right after the SOI marker.
    private static byte[] InjectExifOrientation(byte[] jpegBytes, ushort orientation)
    {
        byte[] tiff =
        [
            0x49, 0x49, 0x2A, 0x00, // "II", magic 42
            0x08, 0x00, 0x00, 0x00, // offset to IFD0
            0x01, 0x00, // 1 entry
            0x12, 0x01, // tag 0x0112 Orientation
            0x03, 0x00, // type SHORT
            0x01, 0x00, 0x00, 0x00, // count 1
            (byte)(orientation & 0xFF), (byte)(orientation >> 8), 0x00, 0x00, // value
            0x00, 0x00, 0x00, 0x00, // next IFD offset
        ];

        var exifHeader = "Exif\0\0"u8.ToArray();
        var payload = exifHeader.Concat(tiff).ToArray();
        var length = (ushort)(payload.Length + 2);

        var app1 = new byte[] { 0xFF, 0xE1, (byte)(length >> 8), (byte)(length & 0xFF) }
            .Concat(payload)
            .ToArray();

        return jpegBytes.Take(2).Concat(app1).Concat(jpegBytes.Skip(2)).ToArray();
    }

    /// Overwrites the height/width fields of the first baseline SOF0 marker without touching scan data,
    /// so header parsing reports the tampered size without a full decode being attempted.
    private static byte[] PatchSof0Dimensions(byte[] jpegBytes, int width, int height)
    {
        var patched = (byte[])jpegBytes.Clone();

        for (var i = 0; i < patched.Length - 1; i++)
        {
            if (patched[i] == 0xFF && patched[i + 1] == 0xC0)
            {
                patched[i + 5] = (byte)(height >> 8);
                patched[i + 6] = (byte)(height & 0xFF);
                patched[i + 7] = (byte)(width >> 8);
                patched[i + 8] = (byte)(width & 0xFF);
                return patched;
            }
        }

        throw new InvalidOperationException("SOF0 marker not found in test fixture.");
    }
}
