using Steward.Application.Tracking.MileageLogs;
using Steward.Domain.Common.Exceptions;
using Steward.Domain.Entities;
using Steward.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Steward.Infrastructure.Tracking.MileageLogs;

public class MileageLogService(StewardDbContext dbContext) : IMileageLogService
{
    public async Task<MileageLogResponse> CreateAsync(
        Guid assetId, CreateMileageLogRequest request, CancellationToken cancellationToken = default)
    {
        var mileageLog = new MileageLog
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            Date = request.Date,
            OdometerReading = request.OdometerReading,
            TripMiles = request.TripMiles,
            Notes = request.Notes,
        };

        dbContext.MileageLogs.Add(mileageLog);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(mileageLog);
    }

    public async Task<IReadOnlyCollection<MileageLogResponse>> ListAsync(
        Guid assetId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var query = dbContext.MileageLogs.AsNoTracking().Where(m => m.AssetId == assetId);

        if (from is { } fromDate)
        {
            query = query.Where(m => m.Date >= fromDate);
        }

        if (to is { } toDate)
        {
            query = query.Where(m => m.Date <= toDate);
        }

        var logs = await query.OrderByDescending(m => m.Date).ToListAsync(cancellationToken);
        return logs.Select(ToResponse).ToList();
    }

    public async Task<MileageLogResponse> UpdateAsync(
        Guid assetId, Guid mileageLogId, UpdateMileageLogRequest request, CancellationToken cancellationToken = default)
    {
        var mileageLog = await FindMileageLogAsync(assetId, mileageLogId, cancellationToken);

        mileageLog.Date = request.Date;
        mileageLog.OdometerReading = request.OdometerReading;
        mileageLog.TripMiles = request.TripMiles;
        mileageLog.Notes = request.Notes;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(mileageLog);
    }

    public async Task DeleteAsync(Guid assetId, Guid mileageLogId, CancellationToken cancellationToken = default)
    {
        var mileageLog = await FindMileageLogAsync(assetId, mileageLogId, cancellationToken);
        dbContext.MileageLogs.Remove(mileageLog);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<MileageLog> FindMileageLogAsync(
        Guid assetId, Guid mileageLogId, CancellationToken cancellationToken)
    {
        return await dbContext.MileageLogs
            .FirstOrDefaultAsync(m => m.Id == mileageLogId && m.AssetId == assetId, cancellationToken)
            ?? throw new NotFoundException("Mileage log not found.");
    }

    private static MileageLogResponse ToResponse(MileageLog mileageLog) => new(
        mileageLog.Id,
        mileageLog.AssetId,
        mileageLog.Date,
        mileageLog.OdometerReading,
        mileageLog.TripMiles,
        mileageLog.Notes);
}
