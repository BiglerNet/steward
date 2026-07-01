using Steward.Application.Tracking.ServiceRecords;
using Steward.Domain.Common.Exceptions;
using Steward.Domain.Entities;
using Steward.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Steward.Infrastructure.Tracking.ServiceRecords;

public class ServiceRecordService(StewardDbContext dbContext) : IServiceRecordService
{
    public async Task<ServiceRecordResponse> CreateAsync(
        Guid assetId, CreateServiceRecordRequest request, CancellationToken cancellationToken = default)
    {
        if (request.EngineId is { } engineId)
        {
            await EnsureEngineBelongsToAssetAsync(assetId, engineId, cancellationToken);
        }

        var serviceRecord = new ServiceRecord
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            EngineId = request.EngineId,
            Date = request.Date,
            Description = request.Description,
            ProviderName = request.ProviderName,
            Cost = request.Cost,
            OdometerMiles = request.OdometerMiles,
            EngineHours = request.EngineHours,
            Notes = request.Notes,
        };

        dbContext.ServiceRecords.Add(serviceRecord);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(serviceRecord);
    }

    public async Task<IReadOnlyCollection<ServiceRecordResponse>> ListAsync(
        Guid assetId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var query = dbContext.ServiceRecords.AsNoTracking().Where(s => s.AssetId == assetId);

        if (from is { } fromDate)
        {
            query = query.Where(s => s.Date >= fromDate);
        }

        if (to is { } toDate)
        {
            query = query.Where(s => s.Date <= toDate);
        }

        var records = await query.OrderByDescending(s => s.Date).ToListAsync(cancellationToken);
        return records.Select(ToResponse).ToList();
    }

    public async Task<ServiceRecordResponse> UpdateAsync(
        Guid assetId,
        Guid serviceRecordId,
        UpdateServiceRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EngineId is { } engineId)
        {
            await EnsureEngineBelongsToAssetAsync(assetId, engineId, cancellationToken);
        }

        var serviceRecord = await FindServiceRecordAsync(assetId, serviceRecordId, cancellationToken);

        serviceRecord.Date = request.Date;
        serviceRecord.Description = request.Description;
        serviceRecord.ProviderName = request.ProviderName;
        serviceRecord.Cost = request.Cost;
        serviceRecord.OdometerMiles = request.OdometerMiles;
        serviceRecord.EngineHours = request.EngineHours;
        serviceRecord.EngineId = request.EngineId;
        serviceRecord.Notes = request.Notes;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(serviceRecord);
    }

    public async Task DeleteAsync(Guid assetId, Guid serviceRecordId, CancellationToken cancellationToken = default)
    {
        var serviceRecord = await FindServiceRecordAsync(assetId, serviceRecordId, cancellationToken);
        dbContext.ServiceRecords.Remove(serviceRecord);
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

    private async Task<ServiceRecord> FindServiceRecordAsync(
        Guid assetId, Guid serviceRecordId, CancellationToken cancellationToken)
    {
        return await dbContext.ServiceRecords
            .FirstOrDefaultAsync(s => s.Id == serviceRecordId && s.AssetId == assetId, cancellationToken)
            ?? throw new NotFoundException("Service record not found.");
    }

    private static ServiceRecordResponse ToResponse(ServiceRecord serviceRecord) => new(
        serviceRecord.Id,
        serviceRecord.AssetId,
        serviceRecord.EngineId,
        serviceRecord.Date,
        serviceRecord.Description,
        serviceRecord.ProviderName,
        serviceRecord.Cost,
        serviceRecord.OdometerMiles,
        serviceRecord.EngineHours,
        serviceRecord.Notes);
}
