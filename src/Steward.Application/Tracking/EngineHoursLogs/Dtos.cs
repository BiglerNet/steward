namespace Steward.Application.Tracking.EngineHoursLogs;

public record EngineHoursLogResponse(
    Guid Id,
    Guid EngineId,
    DateOnly Date,
    decimal? HoursReading,
    decimal? TripHours,
    string? Notes);

public record CreateEngineHoursLogRequest(
    DateOnly Date,
    decimal? HoursReading,
    decimal? TripHours,
    string? Notes);

public record UpdateEngineHoursLogRequest(
    DateOnly Date,
    decimal? HoursReading,
    decimal? TripHours,
    string? Notes);
