using Steward.Application.Assets;
using Steward.Domain.Common.Exceptions;
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
        Guid householdId, AssetType? assetType, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Assets.AsNoTracking().Where(a => a.HouseholdId == householdId);

        if (assetType is { } type)
        {
            var discriminator = type.ToString();
            query = query.Where(a => EF.Property<string>(a, "Discriminator") == discriminator);
        }

        var assets = await query.ToListAsync(cancellationToken);
        return assets.Select(AssetMapper.ToResponse).ToList();
    }

    public async Task<AssetResponse> GetByIdAsync(
        Guid householdId, Guid assetId, CancellationToken cancellationToken = default)
    {
        var asset = await FindAssetAsync(householdId, assetId, cancellationToken);
        return AssetMapper.ToResponse(asset);
    }

    public async Task<AssetResponse> UpdateAsync(
        Guid householdId, Guid assetId, UpdateAssetRequest request, CancellationToken cancellationToken = default)
    {
        var asset = await FindAssetAsync(householdId, assetId, cancellationToken);

        if (request.AssetType is { } requestedType && requestedType != AssetMapper.GetAssetType(asset))
        {
            throw new BadRequestException("assetType cannot be changed after creation.");
        }

        AssetMapper.ApplyUpdate(asset, request);
        asset.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return AssetMapper.ToResponse(asset);
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
}
