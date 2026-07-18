using Steward.Domain.Enums;

namespace Steward.Application.Tracking.MaintenanceRecurrence;

public record MaintenanceScheduleEntryResponse(
    Guid TemplateId,
    string TemplateTitle,
    Guid TemplateStepId,
    string StepText,
    Guid? EngineId,
    string? EngineLabel,
    DateTimeOffset? LastDoneAt,
    MaintenanceReadingResponse? LastDoneReading,
    int? IntervalMonths,
    decimal? IntervalMiles,
    decimal? IntervalHours,
    MaintenanceDueStatus DueStatus);

public record MaintenanceReadingResponse(decimal Value, ReadingUnit Unit);
