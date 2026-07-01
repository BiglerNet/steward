using Steward.Application.Assets.Engines;
using Steward.Domain.Common.Exceptions;
using Steward.Domain.Entities;
using Steward.Domain.Enums;
using Steward.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Steward.Infrastructure.Assets.Engines;

public class EngineService(StewardDbContext dbContext) : IEngineService
{
    public async Task<EngineResponse> CreateAsync(
        Guid assetId, CreateEngineRequest request, CancellationToken cancellationToken = default)
    {
        var engine = new Engine
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            Label = request.Label,
            Make = request.Make,
            Model = request.Model,
            SerialNumber = request.SerialNumber,
            Year = request.Year,
            EngineType = request.EngineType,
            FuelType = request.FuelType,
            Cylinders = request.Cylinders,
            DisplacementCC = request.DisplacementCc,
            Status = EngineStatus.Active,
            InstalledDate = request.InstalledDate,
            InstalledAtAssetMiles = request.InstalledAtAssetMiles,
            InstalledAtAssetHours = request.InstalledAtAssetHours,
            HorsepowerHp = request.HorsepowerHp,
            TorqueNm = request.TorqueNm,
            OilCapacityL = request.OilCapacityL,
            RecommendedOilType = request.RecommendedOilType,
            CoolantCapacityL = request.CoolantCapacityL,
            RecommendedOctane = request.RecommendedOctane,
        };

        dbContext.Engines.Add(engine);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(engine);
    }

    public async Task<IReadOnlyCollection<EngineResponse>> ListAsync(
        Guid assetId, CancellationToken cancellationToken = default)
    {
        var engines = await dbContext.Engines.AsNoTracking()
            .Where(e => e.AssetId == assetId)
            .ToListAsync(cancellationToken);

        return engines.Select(ToResponse).ToList();
    }

    public async Task<EngineResponse> UpdateAsync(
        Guid assetId, Guid engineId, UpdateEngineRequest request, CancellationToken cancellationToken = default)
    {
        var engine = await FindEngineAsync(assetId, engineId, cancellationToken);

        engine.Label = request.Label;
        engine.Make = request.Make;
        engine.Model = request.Model;
        engine.SerialNumber = request.SerialNumber;
        engine.Year = request.Year;
        engine.EngineType = request.EngineType;
        engine.FuelType = request.FuelType;
        engine.Cylinders = request.Cylinders;
        engine.DisplacementCC = request.DisplacementCc;
        engine.InstalledDate = request.InstalledDate;
        engine.InstalledAtAssetMiles = request.InstalledAtAssetMiles;
        engine.InstalledAtAssetHours = request.InstalledAtAssetHours;
        engine.HorsepowerHp = request.HorsepowerHp;
        engine.TorqueNm = request.TorqueNm;
        engine.OilCapacityL = request.OilCapacityL;
        engine.RecommendedOilType = request.RecommendedOilType;
        engine.CoolantCapacityL = request.CoolantCapacityL;
        engine.RecommendedOctane = request.RecommendedOctane;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(engine);
    }

    public async Task<EngineResponse> RetireAsync(
        Guid assetId, Guid engineId, CancellationToken cancellationToken = default)
    {
        var engine = await FindEngineAsync(assetId, engineId, cancellationToken);

        if (engine.Status == EngineStatus.Retired)
        {
            throw new BadRequestException("Engine is already retired.");
        }

        engine.Status = EngineStatus.Retired;
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(engine);
    }

    public async Task<EngineResponse> ReactivateAsync(
        Guid assetId, Guid engineId, CancellationToken cancellationToken = default)
    {
        var engine = await FindEngineAsync(assetId, engineId, cancellationToken);

        if (engine.Status == EngineStatus.Active)
        {
            throw new BadRequestException("Engine is already active.");
        }

        engine.Status = EngineStatus.Active;
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(engine);
    }

    public async Task<EngineResponse> MarkBrokenAsync(
        Guid assetId, Guid engineId, CancellationToken cancellationToken = default)
    {
        var engine = await FindEngineAsync(assetId, engineId, cancellationToken);

        if (engine.Status != EngineStatus.Active)
        {
            throw new BadRequestException("Only an Active engine can be marked Broken.");
        }

        engine.Status = EngineStatus.Broken;
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(engine);
    }

    public async Task DeleteAsync(Guid assetId, Guid engineId, CancellationToken cancellationToken = default)
    {
        var engine = await FindEngineAsync(assetId, engineId, cancellationToken);
        dbContext.Engines.Remove(engine);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid?> GetHouseholdIdForEngineAsync(
        Guid assetId, Guid engineId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Engines.AsNoTracking()
            .Where(e => e.Id == engineId && e.AssetId == assetId)
            .Join(dbContext.Assets.AsNoTracking(), e => e.AssetId, a => a.Id, (e, a) => (Guid?)a.HouseholdId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Engine> FindEngineAsync(Guid assetId, Guid engineId, CancellationToken cancellationToken)
    {
        return await dbContext.Engines
            .FirstOrDefaultAsync(e => e.Id == engineId && e.AssetId == assetId, cancellationToken)
            ?? throw new NotFoundException("Engine not found.");
    }

    private static EngineResponse ToResponse(Engine engine) => new(
        engine.Id,
        engine.AssetId,
        engine.Label,
        engine.Make,
        engine.Model,
        engine.SerialNumber,
        engine.Year,
        engine.EngineType,
        engine.FuelType,
        engine.Cylinders,
        engine.DisplacementCC,
        engine.Status,
        engine.InstalledDate,
        engine.InstalledAtAssetMiles,
        engine.InstalledAtAssetHours,
        engine.HorsepowerHp,
        engine.TorqueNm,
        engine.OilCapacityL,
        engine.RecommendedOilType,
        engine.CoolantCapacityL,
        engine.RecommendedOctane);
}
