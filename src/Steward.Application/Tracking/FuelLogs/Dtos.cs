using Steward.Domain.Enums;

namespace Steward.Application.Tracking.FuelLogs;

public record FuelLogResponse(
    Guid Id,
    Guid AssetId,
    Guid? EngineId,
    FuelLogType LogType,
    DateOnly Date,
    decimal Volume,
    VolumeUnit VolumeUnit,
    string? FuelGrade,
    decimal? PricePerUnit,
    decimal? TotalCost,
    decimal? MilesAtLog,
    decimal? HoursAtLog,
    string? Notes);

public record CreateFuelLogRequest(
    FuelLogType LogType,
    DateOnly Date,
    decimal Volume,
    VolumeUnit VolumeUnit,
    string? FuelGrade,
    decimal? PricePerUnit,
    decimal? TotalCost,
    decimal? MilesAtLog,
    decimal? HoursAtLog,
    Guid? EngineId,
    string? Notes);

public record UpdateFuelLogRequest(
    FuelLogType LogType,
    DateOnly Date,
    decimal Volume,
    VolumeUnit VolumeUnit,
    string? FuelGrade,
    decimal? PricePerUnit,
    decimal? TotalCost,
    decimal? MilesAtLog,
    decimal? HoursAtLog,
    Guid? EngineId,
    string? Notes);
