namespace Steward.Application.Storage;

public record ProcessedPhoto(byte[] ThumbBytes, byte[] DisplayBytes, int Width, int Height);

public interface IImageProcessor
{
    /// Validates, orients, and resizes an uploaded image into thumbnail (&lt;=320px) and
    /// display (&lt;=2048px, never upscaled) JPEG variants. Width/Height describe the display
    /// variant, post-orientation. Throws BadRequestException for unsupported or oversized input.
    ProcessedPhoto Process(Stream input);
}
