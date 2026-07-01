using Steward.Application.Tracking.EngineHoursLogs;
using Steward.Domain.Common.Exceptions;
using Steward.Domain.Entities;
using Steward.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Steward.Infrastructure.Tracking.EngineHoursLogs;

public class EngineHoursLogService(StewardDbContext dbContext) : IEngineHoursLogService
{
    public async Task<EngineHoursLogResponse> CreateAsync(
        Guid engineId, CreateEngineHoursLogRequest request, CancellationToken cancellationToken = default)
    {
        var hoursLog = new EngineHoursLog
        {
            Id = Guid.NewGuid(),
            EngineId = engineId,
            Date = request.Date,
            HoursReading = request.HoursReading,
            TripHours = request.TripHours,
            Notes = request.Notes,
        };

        dbContext.EngineHoursLogs.Add(hoursLog);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(hoursLog);
    }

    public async Task<IReadOnlyCollection<EngineHoursLogResponse>> ListAsync(
        Guid engineId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var query = dbContext.EngineHoursLogs.AsNoTracking().Where(h => h.EngineId == engineId);

        if (from is { } fromDate)
        {
            query = query.Where(h => h.Date >= fromDate);
        }

        if (to is { } toDate)
        {
            query = query.Where(h => h.Date <= toDate);
        }

        var logs = await query.OrderByDescending(h => h.Date).ToListAsync(cancellationToken);
        return logs.Select(ToResponse).ToList();
    }

    public async Task<EngineHoursLogResponse> UpdateAsync(
        Guid engineId, Guid hoursLogId, UpdateEngineHoursLogRequest request, CancellationToken cancellationToken = default)
    {
        var hoursLog = await FindHoursLogAsync(engineId, hoursLogId, cancellationToken);

        hoursLog.Date = request.Date;
        hoursLog.HoursReading = request.HoursReading;
        hoursLog.TripHours = request.TripHours;
        hoursLog.Notes = request.Notes;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(hoursLog);
    }

    public async Task DeleteAsync(Guid engineId, Guid hoursLogId, CancellationToken cancellationToken = default)
    {
        var hoursLog = await FindHoursLogAsync(engineId, hoursLogId, cancellationToken);
        dbContext.EngineHoursLogs.Remove(hoursLog);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<EngineHoursLog> FindHoursLogAsync(
        Guid engineId, Guid hoursLogId, CancellationToken cancellationToken)
    {
        return await dbContext.EngineHoursLogs
            .FirstOrDefaultAsync(h => h.Id == hoursLogId && h.EngineId == engineId, cancellationToken)
            ?? throw new NotFoundException("Engine hours log not found.");
    }

    private static EngineHoursLogResponse ToResponse(EngineHoursLog hoursLog) => new(
        hoursLog.Id,
        hoursLog.EngineId,
        hoursLog.Date,
        hoursLog.HoursReading,
        hoursLog.TripHours,
        hoursLog.Notes);
}
