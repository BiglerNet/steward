namespace Steward.Application.Tracking.MaintenanceRecurrence;

public interface IMaintenanceScheduleService
{
    Task<IReadOnlyList<MaintenanceScheduleEntryResponse>> GetScheduleAsync(
        Guid assetId, CancellationToken cancellationToken = default);
}
