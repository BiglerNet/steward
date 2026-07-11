using Steward.Application.Photos;
using Steward.Application.Storage;
using Steward.Domain.Common.Exceptions;
using Steward.Domain.Entities;
using Steward.Domain.Entities.Assets;
using Steward.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Steward.Infrastructure.Photos;

public class AssetPhotoService(
    StewardDbContext dbContext,
    IFileStorageService fileStorageService,
    IImageProcessor imageProcessor,
    IStorageQuotaService storageQuotaService)
    : IAssetPhotoService
{
    private const string EntityType = "asset-photos";

    public async Task<AssetPhotoResponse> UploadAsync(
        Guid householdId, Guid assetId, Stream content, CancellationToken cancellationToken = default)
    {
        var asset = await dbContext.Assets.FirstOrDefaultAsync(a => a.Id == assetId, cancellationToken)
            ?? throw new NotFoundException("Asset not found.");

        var processed = imageProcessor.Process(content);
        var totalBytes = processed.ThumbBytes.LongLength + processed.DisplayBytes.LongLength;

        await storageQuotaService.EnsureCapacityAsync(householdId, totalBytes, cancellationToken);

        string? thumbKey = null;
        string? displayKey = null;
        try
        {
            thumbKey = await SaveVariantAsync(processed.ThumbBytes, assetId, cancellationToken);
            displayKey = await SaveVariantAsync(processed.DisplayBytes, assetId, cancellationToken);

            var photo = new AssetPhoto
            {
                Id = Guid.NewGuid(),
                AssetId = assetId,
                ThumbStorageKey = thumbKey,
                DisplayStorageKey = displayKey,
                Width = processed.Width,
                Height = processed.Height,
                SizeBytes = totalBytes,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            dbContext.AssetPhotos.Add(photo);

            if (asset.CoverPhotoId is null)
            {
                asset.CoverPhotoId = photo.Id;
            }

            await storageQuotaService.AdjustUsageAsync(householdId, totalBytes, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            return ToResponse(photo);
        }
        catch
        {
            if (thumbKey is not null)
            {
                await fileStorageService.DeleteAsync(thumbKey, cancellationToken);
            }

            if (displayKey is not null)
            {
                await fileStorageService.DeleteAsync(displayKey, cancellationToken);
            }

            throw;
        }
    }

    public async Task<IReadOnlyCollection<AssetPhotoResponse>> ListAsync(
        Guid assetId, CancellationToken cancellationToken = default)
    {
        var photos = await dbContext.AssetPhotos.AsNoTracking()
            .Where(p => p.AssetId == assetId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return photos.Select(ToResponse).ToList();
    }

    public async Task<(Stream Content, string ContentType)> OpenVariantAsync(
        Guid assetId, Guid photoId, string? variant, CancellationToken cancellationToken = default)
    {
        var photo = await dbContext.AssetPhotos.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == photoId && p.AssetId == assetId, cancellationToken)
            ?? throw new NotFoundException("Photo not found.");

        var storageKey = variant switch
        {
            "thumb" => photo.ThumbStorageKey,
            "display" => photo.DisplayStorageKey,
            _ => throw new BadRequestException("variant must be 'thumb' or 'display'."),
        };

        return await fileStorageService.OpenReadAsync(storageKey, cancellationToken);
    }

    public async Task DeleteAsync(
        Guid householdId, Guid assetId, Guid photoId, CancellationToken cancellationToken = default)
    {
        var photo = await dbContext.AssetPhotos
            .FirstOrDefaultAsync(p => p.Id == photoId && p.AssetId == assetId, cancellationToken)
            ?? throw new NotFoundException("Photo not found.");

        var asset = await dbContext.Assets.FirstOrDefaultAsync(a => a.Id == assetId, cancellationToken)
            ?? throw new NotFoundException("Asset not found.");
        var wasCoverPhoto = asset.CoverPhotoId == photo.Id;

        // Capture wasCoverPhoto before Remove(): EF's SetNull cascade fixup nulls the
        // dependent FK in-memory as soon as the principal is marked deleted, so checking
        // asset.CoverPhotoId after this point would always read as already-null.
        dbContext.AssetPhotos.Remove(photo);
        await storageQuotaService.AdjustUsageAsync(householdId, -photo.SizeBytes, cancellationToken);

        if (wasCoverPhoto)
        {
            asset.CoverPhotoId = await dbContext.AssetPhotos.AsNoTracking()
                .Where(p => p.AssetId == assetId && p.Id != photo.Id)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => (Guid?)p.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await fileStorageService.DeleteAsync(photo.ThumbStorageKey, cancellationToken);
        await fileStorageService.DeleteAsync(photo.DisplayStorageKey, cancellationToken);
    }

    public async Task SetCoverAsync(Guid assetId, Guid photoId, CancellationToken cancellationToken = default)
    {
        var asset = await dbContext.Assets.FirstOrDefaultAsync(a => a.Id == assetId, cancellationToken)
            ?? throw new NotFoundException("Asset not found.");

        var belongsToAsset = await dbContext.AssetPhotos
            .AnyAsync(p => p.Id == photoId && p.AssetId == assetId, cancellationToken);
        if (!belongsToAsset)
        {
            throw new BadRequestException("Photo does not belong to this asset.");
        }

        asset.CoverPhotoId = photoId;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> SaveVariantAsync(byte[] bytes, Guid assetId, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(bytes);
        return await fileStorageService.SaveAsync(stream, "image/jpeg", EntityType, assetId, cancellationToken);
    }

    private static AssetPhotoResponse ToResponse(AssetPhoto photo) => new(
        photo.Id, photo.AssetId, photo.Width, photo.Height, photo.SizeBytes, photo.CreatedAt);
}
