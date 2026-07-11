namespace Steward.Application.Photos;

public interface IAssetPhotoService
{
    Task<AssetPhotoResponse> UploadAsync(
        Guid householdId, Guid assetId, Stream content, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AssetPhotoResponse>> ListAsync(
        Guid assetId, CancellationToken cancellationToken = default);

    Task<(Stream Content, string ContentType)> OpenVariantAsync(
        Guid assetId, Guid photoId, string? variant, CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid householdId, Guid assetId, Guid photoId, CancellationToken cancellationToken = default);

    Task SetCoverAsync(
        Guid assetId, Guid photoId, CancellationToken cancellationToken = default);
}
