namespace Steward.Application.Assets.Engines;

public interface IEngineService
{
    Task<EngineResponse> CreateAsync(
        Guid assetId, CreateEngineRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<EngineResponse>> ListAsync(Guid assetId, CancellationToken cancellationToken = default);

    Task<EngineResponse> UpdateAsync(
        Guid assetId, Guid engineId, UpdateEngineRequest request, CancellationToken cancellationToken = default);

    Task<EngineResponse> RetireAsync(Guid assetId, Guid engineId, CancellationToken cancellationToken = default);

    Task<EngineResponse> MarkBrokenAsync(Guid assetId, Guid engineId, CancellationToken cancellationToken = default);

    Task<EngineResponse> ReactivateAsync(Guid assetId, Guid engineId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid assetId, Guid engineId, CancellationToken cancellationToken = default);

    Task<Guid?> GetHouseholdIdForEngineAsync(
        Guid assetId, Guid engineId, CancellationToken cancellationToken = default);
}
