namespace Steward.Application.Tracking.MileageLogs;

public record MileageLogResponse(
    Guid Id,
    Guid AssetId,
    DateOnly Date,
    decimal? OdometerReading,
    decimal? TripMiles,
    string? Notes);

public record CreateMileageLogRequest(
    DateOnly Date,
    decimal? OdometerReading,
    decimal? TripMiles,
    string? Notes);

public record UpdateMileageLogRequest(
    DateOnly Date,
    decimal? OdometerReading,
    decimal? TripMiles,
    string? Notes);
