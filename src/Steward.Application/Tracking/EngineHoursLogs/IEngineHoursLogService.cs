namespace Steward.Application.Tracking.EngineHoursLogs;

public interface IEngineHoursLogService
{
    Task<EngineHoursLogResponse> CreateAsync(
        Guid engineId, CreateEngineHoursLogRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<EngineHoursLogResponse>> ListAsync(
        Guid engineId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);

    Task<EngineHoursLogResponse> UpdateAsync(
        Guid engineId, Guid hoursLogId, UpdateEngineHoursLogRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid engineId, Guid hoursLogId, CancellationToken cancellationToken = default);
}
