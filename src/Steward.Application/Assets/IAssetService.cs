using Steward.Domain.Enums;

namespace Steward.Application.Assets;

public interface IAssetService
{
    Task<AssetResponse> CreateAsync(
        Guid householdId, CreateAssetRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AssetResponse>> ListAsync(
        Guid householdId, AssetCategory? category, AssetGroup? group, CancellationToken cancellationToken = default);

    Task<AssetResponse> GetByIdAsync(
        Guid householdId, Guid assetId, CancellationToken cancellationToken = default);

    Task<AssetResponse> UpdateAsync(
        Guid householdId, Guid assetId, UpdateAssetRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid householdId, Guid assetId, CancellationToken cancellationToken = default);

    Task<Guid?> GetHouseholdIdForAssetAsync(Guid assetId, CancellationToken cancellationToken = default);
}
