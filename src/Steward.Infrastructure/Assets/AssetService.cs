using Steward.Application.Assets;
using Steward.Application.AssetTypes;
using Steward.Domain.Common.Exceptions;
using Steward.Domain.Entities;
using Steward.Domain.Entities.Assets;
using Steward.Domain.Enums;
using Steward.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Steward.Infrastructure.Assets;

public class AssetService(StewardDbContext dbContext) : IAssetService
{
    public async Task<AssetResponse> CreateAsync(
        Guid householdId, CreateAssetRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var asset = AssetMapper.CreateEntity(householdId, request);
        asset.CreatedAt = now;
        asset.UpdatedAt = now;

        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AssetMapper.ToResponse(asset);
    }

    public async Task<IReadOnlyCollection<AssetResponse>> ListAsync(
        Guid householdId, AssetCategory? category, AssetGroup? group, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Assets.AsNoTracking().Where(a => a.HouseholdId == householdId);

        if (category is { } categoryFilter)
        {
            query = query.Where(a => a.Category == categoryFilter);
        }

        if (group is { } groupFilter)
        {
            var categories = AssetTypeRegistry.CategoriesInGroup(groupFilter);
            query = query.Where(a => categories.Contains(a.Category));
        }

        var assets = await query.ToListAsync(cancellationToken);
        var assetIds = assets.Select(a => a.Id).ToList();
        var activeEnginesByAsset = await dbContext.Engines.AsNoTracking()
            .Where(e => assetIds.Contains(e.AssetId) && e.Status == EngineStatus.Active)
            .ToListAsync(cancellationToken);
        var enginesLookup = activeEnginesByAsset.ToLookup(e => e.AssetId);

        return assets.Select(a => AssetMapper.ToResponse(a, enginesLookup[a.Id].ToList())).ToList();
    }

    public async Task<AssetResponse> GetByIdAsync(
        Guid householdId, Guid assetId, CancellationToken cancellationToken = default)
    {
        var asset = await FindAssetAsync(householdId, assetId, cancellationToken);
        var activeEngines = await GetActiveEnginesAsync(assetId, cancellationToken);
        return AssetMapper.ToResponse(asset, activeEngines);
    }

    public async Task<AssetResponse> UpdateAsync(
        Guid householdId, Guid assetId, UpdateAssetRequest request, CancellationToken cancellationToken = default)
    {
        var asset = await FindAssetAsync(householdId, assetId, cancellationToken);

        if (request.Category is { } requestedCategory && requestedCategory != asset.Category)
        {
            throw new BadRequestException("category cannot be changed after creation.");
        }

        var inapplicable = AssetTypeFieldCheck.FindInapplicableFields(asset.Category, request);
        if (inapplicable.Count > 0)
        {
            throw new BadRequestException(AssetTypeFieldCheck.InapplicableMessage(inapplicable[0], asset.Category));
        }

        AssetMapper.ApplyUpdate(asset, request);
        asset.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        var activeEngines = await GetActiveEnginesAsync(assetId, cancellationToken);
        return AssetMapper.ToResponse(asset, activeEngines);
    }

    public async Task DeleteAsync(Guid householdId, Guid assetId, CancellationToken cancellationToken = default)
    {
        var asset = await FindAssetAsync(householdId, assetId, cancellationToken);
        dbContext.Assets.Remove(asset);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid?> GetHouseholdIdForAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Assets.AsNoTracking()
            .Where(a => a.Id == assetId)
            .Select(a => (Guid?)a.HouseholdId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Asset> FindAssetAsync(
        Guid householdId, Guid assetId, CancellationToken cancellationToken)
    {
        return await dbContext.Assets
            .FirstOrDefaultAsync(a => a.Id == assetId && a.HouseholdId == householdId, cancellationToken)
            ?? throw new NotFoundException("Asset not found.");
    }

    private async Task<List<Engine>> GetActiveEnginesAsync(Guid assetId, CancellationToken cancellationToken)
    {
        return await dbContext.Engines.AsNoTracking()
            .Where(e => e.AssetId == assetId && e.Status == EngineStatus.Active)
            .ToListAsync(cancellationToken);
    }
}
