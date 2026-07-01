namespace Steward.Application.Tracking.Warranties;

public interface IWarrantyService
{
    Task<WarrantyResponse> CreateAsync(
        Guid assetId, CreateWarrantyRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WarrantyResponse>> ListAsync(
        Guid assetId, CancellationToken cancellationToken = default);

    Task<WarrantyResponse> UpdateAsync(
        Guid assetId,
        Guid warrantyId,
        UpdateWarrantyRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid assetId, Guid warrantyId, CancellationToken cancellationToken = default);

    Task<WarrantyResponse> SetDocumentAsync(
        Guid assetId, Guid warrantyId, string storageKey, CancellationToken cancellationToken = default);

    Task RemoveDocumentAsync(Guid assetId, Guid warrantyId, CancellationToken cancellationToken = default);

    Task<string?> GetDocumentStorageKeyAsync(
        Guid assetId, Guid warrantyId, CancellationToken cancellationToken = default);
}
