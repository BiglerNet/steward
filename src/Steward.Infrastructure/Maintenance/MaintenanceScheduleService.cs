using Steward.Application.Tracking.MaintenanceRecurrence;
using Steward.Domain.Enums;
using Steward.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Steward.Infrastructure.Maintenance;

public class MaintenanceScheduleService(StewardDbContext dbContext) : IMaintenanceScheduleService
{
    private const int DueSoonWindowDays = 7;
    private const int UpcomingWindowDays = 30;

    public async Task<IReadOnlyList<MaintenanceScheduleEntryResponse>> GetScheduleAsync(
        Guid assetId, CancellationToken cancellationToken = default)
    {
        var checklistData = await (
            from c in dbContext.ChecklistItems.AsNoTracking()
            join m in dbContext.MaintenanceItems.AsNoTracking() on c.MaintenanceItemId equals m.Id
            where m.AssetId == assetId && c.TemplateStepId != null
            select new { c.TemplateStepId, c.EngineId, c.Status, c.ResolvedAt })
            .ToListAsync(cancellationToken);

        if (checklistData.Count == 0) return [];

        var pairs = checklistData
            .GroupBy(c => (TemplateStepId: c.TemplateStepId!.Value, c.EngineId))
            .Select(g => new
            {
                g.Key.TemplateStepId,
                g.Key.EngineId,
                LastDoneAt = g.Where(c => c.Status == ChecklistItemStatus.Done && c.ResolvedAt != null)
                    .Select(c => c.ResolvedAt)
                    .DefaultIfEmpty()
                    .Max(),
            })
            .ToList();

        var stepIds = pairs.Select(p => p.TemplateStepId).Distinct().ToList();
        var steps = await dbContext.TemplateSteps.AsNoTracking()
            .Where(s => stepIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        var templateIds = steps.Values.Select(s => s.TemplateId).Distinct().ToList();
        var templates = await dbContext.Templates.AsNoTracking()
            .Where(t => templateIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Title, cancellationToken);

        var engineIds = pairs.Where(p => p.EngineId != null).Select(p => p.EngineId!.Value).Distinct().ToList();
        var engineLabels = await dbContext.Engines.AsNoTracking()
            .Where(e => engineIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.Label, cancellationToken);

        var hoursLogsByEngine = await dbContext.EngineHoursLogs.AsNoTracking()
            .Where(h => engineIds.Contains(h.EngineId) && h.HoursReading != null)
            .ToListAsync(cancellationToken);
        var hoursLogsLookup = hoursLogsByEngine
            .GroupBy(h => h.EngineId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(h => h.Date).ToList());

        var mileageLogs = await dbContext.MileageLogs.AsNoTracking()
            .Where(m => m.AssetId == assetId && m.OdometerReading != null)
            .OrderByDescending(m => m.Date)
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var entries = new List<MaintenanceScheduleEntryResponse>();

        foreach (var pair in pairs)
        {
            if (!steps.TryGetValue(pair.TemplateStepId, out var step)) continue;
            if (!templates.TryGetValue(step.TemplateId, out var templateTitle)) continue;

            var engineLabel = pair.EngineId is { } engineId ? engineLabels.GetValueOrDefault(engineId) : null;
            var lastDoneAtDate = pair.LastDoneAt is { } resolvedAt ? DateOnly.FromDateTime(resolvedAt.UtcDateTime) : (DateOnly?)null;

            var logs = pair.EngineId is { } scopedEngineId
                ? hoursLogsLookup.GetValueOrDefault(scopedEngineId, [])
                : null;

            var lastDoneReading = lastDoneAtDate is { } doneDate
                ? FindNearestReading(pair.EngineId, logs, mileageLogs, doneDate)
                : null;

            var currentReading = FindCurrentReading(pair.EngineId, logs, mileageLogs);

            var dueStatus = ComputeDueStatus(
                step.EngineScoped,
                step.RecurrenceIntervalMonths,
                step.RecurrenceIntervalMiles,
                step.RecurrenceIntervalHours,
                pair.LastDoneAt,
                lastDoneReading,
                currentReading,
                today);

            entries.Add(new MaintenanceScheduleEntryResponse(
                step.TemplateId,
                templateTitle,
                step.Id,
                step.Text,
                pair.EngineId,
                engineLabel,
                pair.LastDoneAt,
                lastDoneReading,
                step.RecurrenceIntervalMonths,
                step.RecurrenceIntervalMiles,
                step.RecurrenceIntervalHours,
                dueStatus));
        }

        return entries
            .OrderBy(e => e.TemplateTitle)
            .ThenBy(e => e.StepText)
            .ThenBy(e => e.EngineLabel)
            .ToList();
    }

    private static MaintenanceReadingResponse? FindNearestReading(
        Guid? engineId,
        List<Domain.Entities.EngineHoursLog>? hoursLogsDescending,
        List<Domain.Entities.MileageLog> mileageLogsDescending,
        DateOnly onOrBefore)
    {
        if (engineId is not null)
        {
            var log = hoursLogsDescending?.FirstOrDefault(h => h.Date <= onOrBefore);
            return log is null ? null : new MaintenanceReadingResponse(log.HoursReading!.Value, ReadingUnit.Hours);
        }

        var mileageLog = mileageLogsDescending.FirstOrDefault(m => m.Date <= onOrBefore);
        return mileageLog is null ? null : new MaintenanceReadingResponse(mileageLog.OdometerReading!.Value, ReadingUnit.Miles);
    }

    private static MaintenanceReadingResponse? FindCurrentReading(
        Guid? engineId,
        List<Domain.Entities.EngineHoursLog>? hoursLogsDescending,
        List<Domain.Entities.MileageLog> mileageLogsDescending)
    {
        if (engineId is not null)
        {
            var log = hoursLogsDescending?.FirstOrDefault();
            return log is null ? null : new MaintenanceReadingResponse(log.HoursReading!.Value, ReadingUnit.Hours);
        }

        var mileageLog = mileageLogsDescending.FirstOrDefault();
        return mileageLog is null ? null : new MaintenanceReadingResponse(mileageLog.OdometerReading!.Value, ReadingUnit.Miles);
    }

    private static MaintenanceDueStatus ComputeDueStatus(
        bool engineScoped,
        int? intervalMonths,
        decimal? intervalMiles,
        decimal? intervalHours,
        DateTimeOffset? lastDoneAt,
        MaintenanceReadingResponse? lastDoneReading,
        MaintenanceReadingResponse? currentReading,
        DateOnly today)
    {
        var usageInterval = engineScoped ? intervalHours : intervalMiles;
        var hasUsageInterval = usageInterval is not null;
        var hasCalendarInterval = intervalMonths is not null;

        if (!hasUsageInterval && !hasCalendarInterval)
        {
            return MaintenanceDueStatus.OK;
        }

        if (lastDoneAt is null)
        {
            return MaintenanceDueStatus.Overdue;
        }

        if (hasUsageInterval && lastDoneReading is not null && currentReading is not null)
        {
            var threshold = lastDoneReading.Value + usageInterval!.Value;
            if (currentReading.Value >= threshold)
            {
                return MaintenanceDueStatus.Overdue;
            }
        }

        if (hasCalendarInterval)
        {
            var lastDoneDate = DateOnly.FromDateTime(lastDoneAt.Value.UtcDateTime);
            var dueDate = lastDoneDate.AddMonths(intervalMonths!.Value);

            if (dueDate < today) return MaintenanceDueStatus.Overdue;
            if (dueDate <= today.AddDays(DueSoonWindowDays)) return MaintenanceDueStatus.DueSoon;
            if (dueDate <= today.AddDays(UpcomingWindowDays)) return MaintenanceDueStatus.Upcoming;
            return MaintenanceDueStatus.OK;
        }

        return hasUsageInterval ? MaintenanceDueStatus.Unknown : MaintenanceDueStatus.OK;
    }
}
