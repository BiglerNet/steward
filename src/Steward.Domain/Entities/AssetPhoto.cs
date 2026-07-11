namespace Steward.Domain.Entities;

public class AssetPhoto
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public required string ThumbStorageKey { get; set; }
    public required string DisplayStorageKey { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public long SizeBytes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
