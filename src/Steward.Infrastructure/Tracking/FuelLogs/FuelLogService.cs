using Steward.Application.Tracking.FuelLogs;
using Steward.Domain.Common.Exceptions;
using Steward.Domain.Entities;
using Steward.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Steward.Infrastructure.Tracking.FuelLogs;

public class FuelLogService(StewardDbContext dbContext) : IFuelLogService
{
    public async Task<FuelLogResponse> CreateAsync(
        Guid assetId, CreateFuelLogRequest request, CancellationToken cancellationToken = default)
    {
        if (request.EngineId is { } engineId)
        {
            await EnsureEngineBelongsToAssetAsync(assetId, engineId, cancellationToken);
        }

        var fuelLog = new FuelLog
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            EngineId = request.EngineId,
            LogType = request.LogType,
            Date = request.Date,
            Volume = request.Volume,
            VolumeUnit = request.VolumeUnit,
            FuelGrade = request.FuelGrade,
            PricePerUnit = request.PricePerUnit,
            TotalCost = request.TotalCost,
            MilesAtLog = request.MilesAtLog,
            HoursAtLog = request.HoursAtLog,
            Notes = request.Notes,
        };

        dbContext.FuelLogs.Add(fuelLog);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(fuelLog);
    }

    public async Task<IReadOnlyCollection<FuelLogResponse>> ListAsync(
        Guid assetId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var query = dbContext.FuelLogs.AsNoTracking().Where(f => f.AssetId == assetId);

        if (from is { } fromDate)
        {
            query = query.Where(f => f.Date >= fromDate);
        }

        if (to is { } toDate)
        {
            query = query.Where(f => f.Date <= toDate);
        }

        var logs = await query.OrderByDescending(f => f.Date).ToListAsync(cancellationToken);
        return logs.Select(ToResponse).ToList();
    }

    public async Task<FuelLogResponse> UpdateAsync(
        Guid assetId, Guid fuelLogId, UpdateFuelLogRequest request, CancellationToken cancellationToken = default)
    {
        if (request.EngineId is { } engineId)
        {
            await EnsureEngineBelongsToAssetAsync(assetId, engineId, cancellationToken);
        }

        var fuelLog = await FindFuelLogAsync(assetId, fuelLogId, cancellationToken);

        fuelLog.LogType = request.LogType;
        fuelLog.Date = request.Date;
        fuelLog.Volume = request.Volume;
        fuelLog.VolumeUnit = request.VolumeUnit;
        fuelLog.FuelGrade = request.FuelGrade;
        fuelLog.PricePerUnit = request.PricePerUnit;
        fuelLog.TotalCost = request.TotalCost;
        fuelLog.MilesAtLog = request.MilesAtLog;
        fuelLog.HoursAtLog = request.HoursAtLog;
        fuelLog.EngineId = request.EngineId;
        fuelLog.Notes = request.Notes;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(fuelLog);
    }

    public async Task DeleteAsync(Guid assetId, Guid fuelLogId, CancellationToken cancellationToken = default)
    {
        var fuelLog = await FindFuelLogAsync(assetId, fuelLogId, cancellationToken);
        dbContext.FuelLogs.Remove(fuelLog);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureEngineBelongsToAssetAsync(
        Guid assetId, Guid engineId, CancellationToken cancellationToken)
    {
        var belongs = await dbContext.Engines.AsNoTracking()
            .AnyAsync(e => e.Id == engineId && e.AssetId == assetId, cancellationToken);

        if (!belongs)
        {
            throw new BadRequestException("engineId does not belong to the specified asset.");
        }
    }

    private async Task<FuelLog> FindFuelLogAsync(Guid assetId, Guid fuelLogId, CancellationToken cancellationToken)
    {
        return await dbContext.FuelLogs
            .FirstOrDefaultAsync(f => f.Id == fuelLogId && f.AssetId == assetId, cancellationToken)
            ?? throw new NotFoundException("Fuel log not found.");
    }

    private static FuelLogResponse ToResponse(FuelLog fuelLog) => new(
        fuelLog.Id,
        fuelLog.AssetId,
        fuelLog.EngineId,
        fuelLog.LogType,
        fuelLog.Date,
        fuelLog.Volume,
        fuelLog.VolumeUnit,
        fuelLog.FuelGrade,
        fuelLog.PricePerUnit,
        fuelLog.TotalCost,
        fuelLog.MilesAtLog,
        fuelLog.HoursAtLog,
        fuelLog.Notes);
}
