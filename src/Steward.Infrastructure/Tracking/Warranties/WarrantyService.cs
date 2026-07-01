using Steward.Application.Storage;
using Steward.Application.Tracking.Warranties;
using Steward.Domain.Common.Exceptions;
using Steward.Domain.Entities;
using Steward.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Steward.Infrastructure.Tracking.Warranties;

public class WarrantyService(StewardDbContext dbContext, IFileStorageService fileStorageService)
    : IWarrantyService
{
    public async Task<WarrantyResponse> CreateAsync(
        Guid assetId, CreateWarrantyRequest request, CancellationToken cancellationToken = default)
    {
        var warranty = new Warranty
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            Provider = request.Provider,
            Description = request.Description,
            StartsOn = request.StartsOn,
            ExpiresOn = request.ExpiresOn,
            Notes = request.Notes,
        };

        dbContext.Warranties.Add(warranty);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(warranty);
    }

    public async Task<IReadOnlyCollection<WarrantyResponse>> ListAsync(
        Guid assetId, CancellationToken cancellationToken = default)
    {
        var warranties = await dbContext.Warranties.AsNoTracking()
            .Where(w => w.AssetId == assetId)
            .ToListAsync(cancellationToken);

        return warranties.Select(ToResponse).ToList();
    }

    public async Task<WarrantyResponse> UpdateAsync(
        Guid assetId,
        Guid warrantyId,
        UpdateWarrantyRequest request,
        CancellationToken cancellationToken = default)
    {
        var warranty = await FindWarrantyAsync(assetId, warrantyId, cancellationToken);

        warranty.Provider = request.Provider;
        warranty.Description = request.Description;
        warranty.StartsOn = request.StartsOn;
        warranty.ExpiresOn = request.ExpiresOn;
        warranty.Notes = request.Notes;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(warranty);
    }

    public async Task DeleteAsync(Guid assetId, Guid warrantyId, CancellationToken cancellationToken = default)
    {
        var warranty = await FindWarrantyAsync(assetId, warrantyId, cancellationToken);

        if (warranty.DocumentUrl is { } storageKey)
        {
            await fileStorageService.DeleteAsync(storageKey, cancellationToken);
        }

        dbContext.Warranties.Remove(warranty);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<WarrantyResponse> SetDocumentAsync(
        Guid assetId, Guid warrantyId, string storageKey, CancellationToken cancellationToken = default)
    {
        var warranty = await FindWarrantyAsync(assetId, warrantyId, cancellationToken);
        var previousStorageKey = warranty.DocumentUrl;

        warranty.DocumentUrl = storageKey;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (previousStorageKey is not null)
        {
            await fileStorageService.DeleteAsync(previousStorageKey, cancellationToken);
        }

        return ToResponse(warranty);
    }

    public async Task RemoveDocumentAsync(Guid assetId, Guid warrantyId, CancellationToken cancellationToken = default)
    {
        var warranty = await FindWarrantyAsync(assetId, warrantyId, cancellationToken);

        if (warranty.DocumentUrl is { } storageKey)
        {
            warranty.DocumentUrl = null;
            await dbContext.SaveChangesAsync(cancellationToken);
            await fileStorageService.DeleteAsync(storageKey, cancellationToken);
        }
    }

    public async Task<string?> GetDocumentStorageKeyAsync(
        Guid assetId, Guid warrantyId, CancellationToken cancellationToken = default)
    {
        var warranty = await FindWarrantyAsync(assetId, warrantyId, cancellationToken);
        return warranty.DocumentUrl;
    }

    private async Task<Warranty> FindWarrantyAsync(
        Guid assetId, Guid warrantyId, CancellationToken cancellationToken)
    {
        return await dbContext.Warranties
            .FirstOrDefaultAsync(w => w.Id == warrantyId && w.AssetId == assetId, cancellationToken)
            ?? throw new NotFoundException("Warranty not found.");
    }

    private static WarrantyResponse ToResponse(Warranty warranty) => new(
        warranty.Id,
        warranty.AssetId,
        warranty.Provider,
        warranty.Description,
        warranty.StartsOn,
        warranty.ExpiresOn,
        warranty.Notes,
        warranty.DocumentUrl is not null,
        DocumentUrl: null);
}
