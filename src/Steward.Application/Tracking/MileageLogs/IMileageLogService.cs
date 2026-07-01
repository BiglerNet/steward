namespace Steward.Application.Tracking.MileageLogs;

public interface IMileageLogService
{
    Task<MileageLogResponse> CreateAsync(
        Guid assetId, CreateMileageLogRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<MileageLogResponse>> ListAsync(
        Guid assetId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);

    Task<MileageLogResponse> UpdateAsync(
        Guid assetId, Guid mileageLogId, UpdateMileageLogRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid assetId, Guid mileageLogId, CancellationToken cancellationToken = default);
}
