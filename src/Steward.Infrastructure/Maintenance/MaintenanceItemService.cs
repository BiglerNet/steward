using Steward.Application.Common;
using Steward.Application.Tracking.MaintenanceItems;
using Steward.Domain.Common.Exceptions;
using Steward.Domain.Entities;
using Steward.Domain.Enums;
using Steward.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Steward.Infrastructure.Maintenance;

public class MaintenanceItemService(StewardDbContext dbContext) : IMaintenanceItemService
{
    public async Task<MaintenanceItemResponse> CreateAsync(
        Guid assetId, CreateMaintenanceItemRequest request, CancellationToken cancellationToken = default)
    {
        if (request.EngineId is { } engineId)
        {
            await EnsureEngineBelongsToAssetAsync(assetId, engineId, cancellationToken);
        }

        var item = new MaintenanceItem
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            EngineId = request.EngineId,
            TemplateId = request.TemplateId,
            Title = request.Title,
            Description = request.Description,
            ProviderName = request.ProviderName,
            Status = request.Status ?? MaintenanceItemStatus.Planned,
            Date = request.Date,
            Cost = request.Cost,
            OdometerMiles = request.OdometerMiles,
            EngineHours = request.EngineHours,
            CreatedAt = DateTimeOffset.UtcNow,
            CompletedAt = request.Status == MaintenanceItemStatus.Done ? DateTimeOffset.UtcNow : null,
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (request.TemplateId is { } templateId)
        {
            var template = await dbContext.Templates
                .Include(t => t.Steps.OrderBy(s => s.SortOrder))
                .ThenInclude(s => s.SuggestedParts)
                .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken)
                ?? throw new NotFoundException("Template not found.");

            var asset = await dbContext.Assets.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == assetId, cancellationToken)
                ?? throw new NotFoundException("Asset not found.");

            if (template.ApplicableCategories.Count > 0 && !template.ApplicableCategories.Contains(asset.Category))
            {
                throw new BadRequestException("Template is not applicable to this asset's category.");
            }

            var activeEngineIds = await dbContext.Engines.AsNoTracking()
                .Where(e => e.AssetId == assetId && e.Status == EngineStatus.Active)
                .OrderBy(e => e.Label).ThenBy(e => e.Id)
                .Select(e => (Guid?)e.Id)
                .ToListAsync(cancellationToken);

            var sortOrder = 0;
            foreach (var step in template.Steps)
            {
                var engineIdsForStep = step.EngineScoped ? activeEngineIds : [(Guid?)null];

                foreach (var stepEngineId in engineIdsForStep)
                {
                    var checklistItem = new ChecklistItem
                    {
                        Id = Guid.NewGuid(),
                        MaintenanceItemId = item.Id,
                        Text = step.Text,
                        SortOrder = sortOrder++,
                        EngineId = stepEngineId,
                        TemplateStepId = step.Id,
                    };
                    item.ChecklistItems.Add(checklistItem);

                    foreach (var suggestedPart in step.SuggestedParts.OrderBy(sp => sp.SortOrder))
                    {
                        item.PartLines.Add(new PartLine
                        {
                            Id = Guid.NewGuid(),
                            MaintenanceItemId = item.Id,
                            Name = suggestedPart.Name,
                            Quantity = suggestedPart.Quantity,
                            Status = PartLineStatus.Needed,
                            ChecklistItemId = checklistItem.Id,
                        });
                    }
                }
            }
        }

        dbContext.MaintenanceItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ToResponse(item);
    }

    public async Task<IReadOnlyCollection<MaintenanceItemResponse>> ListAsync(
        Guid assetId, IReadOnlyCollection<MaintenanceItemStatus>? statuses, CancellationToken cancellationToken = default)
    {
        var query = dbContext.MaintenanceItems.AsNoTracking()
            .Include(m => m.ChecklistItems)
            .Include(m => m.PartLines)
            .Where(m => m.AssetId == assetId);

        if (statuses is { Count: > 0 })
        {
            query = query.Where(m => statuses.Contains(m.Status));
        }

        var items = await query
            .OrderByDescending(m => m.Date == null)
            .ThenByDescending(m => m.Date)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        return items.Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyCollection<HouseholdMaintenanceItemResponse>> ListForHouseholdAsync(
        Guid householdId,
        IReadOnlyCollection<MaintenanceItemStatus>? statuses,
        Guid? assetId,
        CancellationToken cancellationToken = default)
    {
        var query =
            from m in dbContext.MaintenanceItems.AsNoTracking()
            join a in dbContext.Assets.AsNoTracking() on m.AssetId equals a.Id
            where a.HouseholdId == householdId
            select new { MaintenanceItem = m, AssetName = a.Name };

        if (statuses is { Count: > 0 })
        {
            query = query.Where(x => statuses.Contains(x.MaintenanceItem.Status));
        }

        if (assetId is { } filterAssetId)
        {
            query = query.Where(x => x.MaintenanceItem.AssetId == filterAssetId);
        }

        var rows = await query
            .OrderByDescending(x => x.MaintenanceItem.Date == null)
            .ThenByDescending(x => x.MaintenanceItem.Date)
            .ThenByDescending(x => x.MaintenanceItem.CreatedAt)
            .ToListAsync(cancellationToken);

        var maintenanceItemIds = rows.Select(x => x.MaintenanceItem.Id).ToList();

        var checklistItemsByItem = (await dbContext.ChecklistItems.AsNoTracking()
            .Where(c => maintenanceItemIds.Contains(c.MaintenanceItemId))
            .ToListAsync(cancellationToken))
            .GroupBy(c => c.MaintenanceItemId)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.SortOrder).ToList());

        var partLinesByItem = (await dbContext.PartLines.AsNoTracking()
            .Where(p => maintenanceItemIds.Contains(p.MaintenanceItemId))
            .ToListAsync(cancellationToken))
            .GroupBy(p => p.MaintenanceItemId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return rows.Select(x =>
        {
            var checklistItems = checklistItemsByItem.GetValueOrDefault(x.MaintenanceItem.Id, []);
            var partLines = partLinesByItem.GetValueOrDefault(x.MaintenanceItem.Id, []);
            var isBlocked = partLines.Any(p => p.Status is PartLineStatus.Needed or PartLineStatus.Ordered);

            return new HouseholdMaintenanceItemResponse(
                x.MaintenanceItem.Id,
                x.MaintenanceItem.AssetId,
                x.AssetName,
                x.MaintenanceItem.EngineId,
                x.MaintenanceItem.TemplateId,
                x.MaintenanceItem.Title,
                x.MaintenanceItem.Description,
                x.MaintenanceItem.ProviderName,
                x.MaintenanceItem.Status,
                x.MaintenanceItem.Date,
                x.MaintenanceItem.Cost,
                x.MaintenanceItem.OdometerMiles,
                x.MaintenanceItem.EngineHours,
                isBlocked,
                x.MaintenanceItem.CompletedAt,
                checklistItems.Select(ToChecklistResponse).ToList(),
                partLines.Select(ToPartLineResponse).ToList());
        }).ToList();
    }

    public async Task<MaintenanceItemResponse> GetAsync(
        Guid assetId, Guid maintenanceItemId, CancellationToken cancellationToken = default)
    {
        var item = await FindMaintenanceItemWithChildrenAsync(assetId, maintenanceItemId, cancellationToken);
        return ToResponse(item);
    }

    public async Task<MaintenanceItemResponse> PatchAsync(
        Guid assetId, Guid maintenanceItemId, PatchMaintenanceItemRequest request, CancellationToken cancellationToken = default)
    {
        var item = await FindMaintenanceItemWithChildrenAsync(assetId, maintenanceItemId, cancellationToken);

        if (request.EngineId is { IsSet: true, Value: { } engineId })
        {
            await EnsureEngineBelongsToAssetAsync(assetId, engineId, cancellationToken);
        }

        request.Title.IfSet(v => item.Title = v!);
        request.Description.IfSet(v => item.Description = v);
        request.ProviderName.IfSet(v => item.ProviderName = v);
        request.Status.IfSet(v =>
        {
            item.Status = v;
            item.CompletedAt = v == MaintenanceItemStatus.Done ? DateTimeOffset.UtcNow : null;
        });
        request.Date.IfSet(v => item.Date = v);
        request.Cost.IfSet(v => item.Cost = v);
        request.OdometerMiles.IfSet(v => item.OdometerMiles = v);
        request.EngineHours.IfSet(v => item.EngineHours = v);
        request.EngineId.IfSet(v => item.EngineId = v);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(item);
    }

    public async Task DeleteAsync(Guid assetId, Guid maintenanceItemId, CancellationToken cancellationToken = default)
    {
        var item = await FindMaintenanceItemAsync(assetId, maintenanceItemId, cancellationToken);
        dbContext.MaintenanceItems.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ChecklistItemResponse> CreateChecklistItemAsync(
        Guid assetId, Guid maintenanceItemId, CreateChecklistItemRequest request, CancellationToken cancellationToken = default)
    {
        await FindMaintenanceItemAsync(assetId, maintenanceItemId, cancellationToken);

        if (request.EngineId is { } engineId)
        {
            await EnsureEngineBelongsToAssetAsync(assetId, engineId, cancellationToken);
        }

        var nextSortOrder = await dbContext.ChecklistItems.AsNoTracking()
            .Where(c => c.MaintenanceItemId == maintenanceItemId)
            .Select(c => (int?)c.SortOrder)
            .MaxAsync(cancellationToken) is { } max ? max + 1 : 0;

        var checklistItem = new ChecklistItem
        {
            Id = Guid.NewGuid(),
            MaintenanceItemId = maintenanceItemId,
            Text = request.Text,
            Status = ChecklistItemStatus.Open,
            SortOrder = nextSortOrder,
            EngineId = request.EngineId,
        };

        dbContext.ChecklistItems.Add(checklistItem);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToChecklistResponse(checklistItem);
    }

    public async Task<ChecklistItemResponse> PatchChecklistItemAsync(
        Guid assetId,
        Guid maintenanceItemId,
        Guid checklistItemId,
        PatchChecklistItemRequest request,
        CancellationToken cancellationToken = default)
    {
        await FindMaintenanceItemAsync(assetId, maintenanceItemId, cancellationToken);
        var checklistItem = await FindChecklistItemAsync(maintenanceItemId, checklistItemId, cancellationToken);

        if (request.EngineId is { IsSet: true, Value: { } engineId })
        {
            await EnsureEngineBelongsToAssetAsync(assetId, engineId, cancellationToken);
        }

        request.Text.IfSet(v => checklistItem.Text = v!);
        request.EngineId.IfSet(v => checklistItem.EngineId = v);
        request.Status.IfSet(v =>
        {
            checklistItem.Status = v;
            checklistItem.ResolvedAt = v == ChecklistItemStatus.Open ? null : DateTimeOffset.UtcNow;
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToChecklistResponse(checklistItem);
    }

    public async Task DeleteChecklistItemAsync(
        Guid assetId, Guid maintenanceItemId, Guid checklistItemId, CancellationToken cancellationToken = default)
    {
        await FindMaintenanceItemAsync(assetId, maintenanceItemId, cancellationToken);
        var checklistItem = await FindChecklistItemAsync(maintenanceItemId, checklistItemId, cancellationToken);
        dbContext.ChecklistItems.Remove(checklistItem);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChecklistItemResponse>> ReorderChecklistItemsAsync(
        Guid assetId, Guid maintenanceItemId, ReorderChecklistItemsRequest request, CancellationToken cancellationToken = default)
    {
        await FindMaintenanceItemAsync(assetId, maintenanceItemId, cancellationToken);

        var checklistItems = await dbContext.ChecklistItems
            .Where(c => c.MaintenanceItemId == maintenanceItemId)
            .ToListAsync(cancellationToken);

        var existingIds = checklistItems.Select(c => c.Id).ToHashSet();
        if (request.ChecklistItemIds.Count != existingIds.Count || !request.ChecklistItemIds.ToHashSet().SetEquals(existingIds))
        {
            throw new BadRequestException("Reorder request must include exactly the maintenance item's existing checklist item ids.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var byId = checklistItems.ToDictionary(c => c.Id);
        for (var index = 0; index < request.ChecklistItemIds.Count; index++)
        {
            byId[request.ChecklistItemIds[index]].SortOrder = index;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return checklistItems.OrderBy(c => c.SortOrder).Select(ToChecklistResponse).ToList();
    }

    public async Task<PartLineResponse> CreatePartLineAsync(
        Guid assetId, Guid maintenanceItemId, CreatePartLineRequest request, CancellationToken cancellationToken = default)
    {
        await FindMaintenanceItemAsync(assetId, maintenanceItemId, cancellationToken);

        if (request.ChecklistItemId is { } checklistItemId)
        {
            await EnsureChecklistItemBelongsToItemAsync(maintenanceItemId, checklistItemId, cancellationToken);
        }

        var partLine = new PartLine
        {
            Id = Guid.NewGuid(),
            MaintenanceItemId = maintenanceItemId,
            Name = request.Name,
            PartNumber = request.PartNumber,
            Vendor = request.Vendor,
            TrackingNumber = request.TrackingNumber,
            OrderUrl = request.OrderUrl,
            Quantity = request.Quantity ?? 1,
            Status = PartLineStatus.Needed,
            Cost = request.Cost,
            ChecklistItemId = request.ChecklistItemId,
        };

        dbContext.PartLines.Add(partLine);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToPartLineResponse(partLine);
    }

    public async Task<PartLineResponse> PatchPartLineAsync(
        Guid assetId, Guid maintenanceItemId, Guid partLineId, PatchPartLineRequest request, CancellationToken cancellationToken = default)
    {
        await FindMaintenanceItemAsync(assetId, maintenanceItemId, cancellationToken);
        var partLine = await FindPartLineAsync(maintenanceItemId, partLineId, cancellationToken);

        if (request.ChecklistItemId is { IsSet: true, Value: { } checklistItemId })
        {
            await EnsureChecklistItemBelongsToItemAsync(maintenanceItemId, checklistItemId, cancellationToken);
        }

        request.Name.IfSet(v => partLine.Name = v!);
        request.PartNumber.IfSet(v => partLine.PartNumber = v);
        request.Vendor.IfSet(v => partLine.Vendor = v);
        request.TrackingNumber.IfSet(v => partLine.TrackingNumber = v);
        request.OrderUrl.IfSet(v => partLine.OrderUrl = v);
        request.Quantity.IfSet(v => partLine.Quantity = v);
        request.Status.IfSet(v => partLine.Status = v);
        request.Cost.IfSet(v => partLine.Cost = v);
        request.ChecklistItemId.IfSet(v => partLine.ChecklistItemId = v);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToPartLineResponse(partLine);
    }

    public async Task DeletePartLineAsync(
        Guid assetId, Guid maintenanceItemId, Guid partLineId, CancellationToken cancellationToken = default)
    {
        await FindMaintenanceItemAsync(assetId, maintenanceItemId, cancellationToken);
        var partLine = await FindPartLineAsync(maintenanceItemId, partLineId, cancellationToken);
        dbContext.PartLines.Remove(partLine);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureEngineBelongsToAssetAsync(Guid assetId, Guid engineId, CancellationToken cancellationToken)
    {
        var belongs = await dbContext.Engines.AsNoTracking()
            .AnyAsync(e => e.Id == engineId && e.AssetId == assetId, cancellationToken);

        if (!belongs)
        {
            throw new BadRequestException("engineId does not belong to the specified asset.");
        }
    }

    private async Task EnsureChecklistItemBelongsToItemAsync(
        Guid maintenanceItemId, Guid checklistItemId, CancellationToken cancellationToken)
    {
        var belongs = await dbContext.ChecklistItems.AsNoTracking()
            .AnyAsync(c => c.Id == checklistItemId && c.MaintenanceItemId == maintenanceItemId, cancellationToken);

        if (!belongs)
        {
            throw new BadRequestException("checklistItemId does not belong to the specified maintenance item.");
        }
    }

    private async Task<MaintenanceItem> FindMaintenanceItemAsync(
        Guid assetId, Guid maintenanceItemId, CancellationToken cancellationToken)
    {
        return await dbContext.MaintenanceItems
            .FirstOrDefaultAsync(m => m.Id == maintenanceItemId && m.AssetId == assetId, cancellationToken)
            ?? throw new NotFoundException("Maintenance item not found.");
    }

    private async Task<MaintenanceItem> FindMaintenanceItemWithChildrenAsync(
        Guid assetId, Guid maintenanceItemId, CancellationToken cancellationToken)
    {
        return await dbContext.MaintenanceItems
            .Include(m => m.ChecklistItems)
            .Include(m => m.PartLines)
            .FirstOrDefaultAsync(m => m.Id == maintenanceItemId && m.AssetId == assetId, cancellationToken)
            ?? throw new NotFoundException("Maintenance item not found.");
    }

    private async Task<ChecklistItem> FindChecklistItemAsync(
        Guid maintenanceItemId, Guid checklistItemId, CancellationToken cancellationToken)
    {
        return await dbContext.ChecklistItems
            .FirstOrDefaultAsync(c => c.Id == checklistItemId && c.MaintenanceItemId == maintenanceItemId, cancellationToken)
            ?? throw new NotFoundException("Checklist item not found.");
    }

    private async Task<PartLine> FindPartLineAsync(
        Guid maintenanceItemId, Guid partLineId, CancellationToken cancellationToken)
    {
        return await dbContext.PartLines
            .FirstOrDefaultAsync(p => p.Id == partLineId && p.MaintenanceItemId == maintenanceItemId, cancellationToken)
            ?? throw new NotFoundException("Part line not found.");
    }

    private static MaintenanceItemResponse ToResponse(MaintenanceItem item)
    {
        var isBlocked = item.PartLines.Any(p => p.Status is PartLineStatus.Needed or PartLineStatus.Ordered);

        return new MaintenanceItemResponse(
            item.Id,
            item.AssetId,
            item.EngineId,
            item.TemplateId,
            item.Title,
            item.Description,
            item.ProviderName,
            item.Status,
            item.Date,
            item.Cost,
            item.OdometerMiles,
            item.EngineHours,
            isBlocked,
            item.CompletedAt,
            item.ChecklistItems.OrderBy(c => c.SortOrder).Select(ToChecklistResponse).ToList(),
            item.PartLines.Select(ToPartLineResponse).ToList());
    }

    private static ChecklistItemResponse ToChecklistResponse(ChecklistItem checklistItem) => new(
        checklistItem.Id,
        checklistItem.MaintenanceItemId,
        checklistItem.Text,
        checklistItem.Status,
        checklistItem.ResolvedAt,
        checklistItem.SortOrder,
        checklistItem.EngineId,
        checklistItem.TemplateStepId);

    private static PartLineResponse ToPartLineResponse(PartLine partLine) => new(
        partLine.Id,
        partLine.MaintenanceItemId,
        partLine.Name,
        partLine.PartNumber,
        partLine.Vendor,
        partLine.TrackingNumber,
        partLine.OrderUrl,
        partLine.Quantity,
        partLine.Status,
        partLine.Cost,
        partLine.ChecklistItemId,
        partLine.PartId);
}
