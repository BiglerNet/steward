namespace Steward.Application.Tracking.FuelLogs;

public interface IFuelLogService
{
    Task<FuelLogResponse> CreateAsync(
        Guid assetId, CreateFuelLogRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<FuelLogResponse>> ListAsync(
        Guid assetId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);

    Task<FuelLogResponse> UpdateAsync(
        Guid assetId, Guid fuelLogId, UpdateFuelLogRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid assetId, Guid fuelLogId, CancellationToken cancellationToken = default);
}
