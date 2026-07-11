namespace Steward.Application.Photos;

public record AssetPhotoResponse(
    Guid Id,
    Guid AssetId,
    int Width,
    int Height,
    long SizeBytes,
    DateTimeOffset CreatedAt);

public record SetCoverPhotoRequest(Guid PhotoId);
