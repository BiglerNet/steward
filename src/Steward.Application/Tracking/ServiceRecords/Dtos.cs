namespace Steward.Application.Tracking.ServiceRecords;

public record ServiceRecordResponse(
    Guid Id,
    Guid AssetId,
    Guid? EngineId,
    DateOnly Date,
    string Description,
    string? ProviderName,
    decimal? Cost,
    decimal? OdometerMiles,
    decimal? EngineHours,
    string? Notes);

public record CreateServiceRecordRequest(
    DateOnly Date,
    string Description,
    string? ProviderName,
    decimal? Cost,
    decimal? OdometerMiles,
    decimal? EngineHours,
    Guid? EngineId,
    string? Notes);

public record UpdateServiceRecordRequest(
    DateOnly Date,
    string Description,
    string? ProviderName,
    decimal? Cost,
    decimal? OdometerMiles,
    decimal? EngineHours,
    Guid? EngineId,
    string? Notes);
