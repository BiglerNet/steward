using System.Text.Json;
using Steward.Application.Dashboards;
using Steward.Domain.Common.Exceptions;
using Steward.Domain.Entities;
using Steward.Domain.Enums;
using Steward.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Steward.Infrastructure.Dashboards;

public class DashboardService(StewardDbContext dbContext) : IDashboardService
{
    public async Task<IReadOnlyList<DashboardSummaryResponse>> ListAsync(
        Guid householdId, CancellationToken cancellationToken = default)
    {
        var dashboards = await dbContext.HouseholdDashboards.AsNoTracking()
            .Where(d => d.HouseholdId == householdId)
            .OrderBy(d => d.Position)
            .Select(d => new DashboardSummaryResponse(d.Id, d.Name, d.IsDefault, d.Position))
            .ToListAsync(cancellationToken);

        if (dashboards.Count == 0)
        {
            var seeded = await SeedDefaultDashboardAsync(householdId, cancellationToken);
            return [seeded];
        }

        return dashboards;
    }

    public async Task<DashboardDetailResponse> GetAsync(
        Guid householdId, Guid dashboardId, CancellationToken cancellationToken = default)
    {
        var dashboard = await dbContext.HouseholdDashboards.AsNoTracking()
            .Where(d => d.Id == dashboardId && d.HouseholdId == householdId)
            .Select(d => new DashboardDetailResponse(
                d.Id,
                d.Name,
                d.IsDefault,
                d.Position,
                d.Widgets.OrderBy(w => w.Position)
                    .Select(w => new WidgetResponse(w.Id, w.WidgetType, w.WidgetSize, w.Position, w.Config))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Dashboard not found.");

        return dashboard;
    }

    public async Task<DashboardSummaryResponse> CreateAsync(
        Guid householdId, CreateDashboardRequest request, CancellationToken cancellationToken = default)
    {
        var nameExists = await dbContext.HouseholdDashboards
            .AnyAsync(d => d.HouseholdId == householdId
                && d.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (nameExists)
        {
            throw new BadRequestException($"A dashboard named '{request.Name}' already exists in this household.");
        }

        var isDefault = request.IsDefault ?? false;

        if (isDefault)
        {
            await DemoteCurrentDefaultAsync(householdId, null, cancellationToken);
        }

        var dashboard = new HouseholdDashboard
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            Name = request.Name,
            IsDefault = isDefault,
            Position = await GetNextPositionAsync(householdId, cancellationToken),
        };

        dbContext.HouseholdDashboards.Add(dashboard);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new DashboardSummaryResponse(dashboard.Id, dashboard.Name, dashboard.IsDefault, dashboard.Position);
    }

    public async Task<DashboardSummaryResponse> UpdateAsync(
        Guid householdId, Guid dashboardId, UpdateDashboardRequest request, CancellationToken cancellationToken = default)
    {
        var dashboard = await dbContext.HouseholdDashboards
            .FirstOrDefaultAsync(d => d.Id == dashboardId && d.HouseholdId == householdId, cancellationToken)
            ?? throw new NotFoundException("Dashboard not found.");

        var nameConflict = await dbContext.HouseholdDashboards
            .AnyAsync(d => d.HouseholdId == householdId
                && d.Id != dashboardId
                && d.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (nameConflict)
        {
            throw new BadRequestException($"A dashboard named '{request.Name}' already exists in this household.");
        }

        if (request.IsDefault && !dashboard.IsDefault)
        {
            await DemoteCurrentDefaultAsync(householdId, dashboardId, cancellationToken);
        }

        dashboard.Name = request.Name;
        dashboard.IsDefault = request.IsDefault;
        dashboard.Position = request.Position;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new DashboardSummaryResponse(dashboard.Id, dashboard.Name, dashboard.IsDefault, dashboard.Position);
    }

    public async Task DeleteAsync(Guid householdId, Guid dashboardId, CancellationToken cancellationToken = default)
    {
        var count = await dbContext.HouseholdDashboards
            .CountAsync(d => d.HouseholdId == householdId, cancellationToken);

        if (count <= 1)
        {
            throw new BadRequestException("Cannot delete the last remaining dashboard.");
        }

        var dashboard = await dbContext.HouseholdDashboards
            .FirstOrDefaultAsync(d => d.Id == dashboardId && d.HouseholdId == householdId, cancellationToken)
            ?? throw new NotFoundException("Dashboard not found.");

        dbContext.HouseholdDashboards.Remove(dashboard);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<DashboardDetailResponse> ReplaceWidgetLayoutAsync(
        Guid householdId, Guid dashboardId, ReplaceWidgetLayoutRequest request, CancellationToken cancellationToken = default)
    {
        var dashboard = await dbContext.HouseholdDashboards
            .Include(d => d.Widgets)
            .FirstOrDefaultAsync(d => d.Id == dashboardId && d.HouseholdId == householdId, cancellationToken)
            ?? throw new NotFoundException("Dashboard not found.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        dbContext.DashboardWidgets.RemoveRange(dashboard.Widgets);
        await dbContext.SaveChangesAsync(cancellationToken);

        var newWidgets = request.Widgets
            .Select((def, index) => new DashboardWidget
            {
                Id = Guid.NewGuid(),
                DashboardId = dashboardId,
                WidgetType = def.WidgetType,
                WidgetSize = def.WidgetSize,
                Position = index,
                Config = def.Config,
            })
            .ToList();

        dbContext.DashboardWidgets.AddRange(newWidgets);
        await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new DashboardDetailResponse(
            dashboard.Id,
            dashboard.Name,
            dashboard.IsDefault,
            dashboard.Position,
            newWidgets.Select(w => new WidgetResponse(w.Id, w.WidgetType, w.WidgetSize, w.Position, w.Config)).ToList());
    }

    public async Task<Dictionary<string, object>> GetSnapshotAsync(
        Guid householdId, Guid dashboardId, CancellationToken cancellationToken = default)
    {
        var widgetTypes = await dbContext.HouseholdDashboards.AsNoTracking()
            .Where(d => d.Id == dashboardId && d.HouseholdId == householdId)
            .SelectMany(d => d.Widgets.Select(w => new { w.WidgetType, w.Config }))
            .ToListAsync(cancellationToken);

        if (widgetTypes.Count == 0)
        {
            // Verify the dashboard exists (could be empty or not found)
            var exists = await dbContext.HouseholdDashboards.AsNoTracking()
                .AnyAsync(d => d.Id == dashboardId && d.HouseholdId == householdId, cancellationToken);

            if (!exists) throw new NotFoundException("Dashboard not found.");

            return [];
        }

        var activeWidgetTypes = widgetTypes.Select(w => w.WidgetType).Distinct().ToHashSet();
        var result = new Dictionary<string, object>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (activeWidgetTypes.Contains(WidgetType.AssetCount))
        {
            var count = await dbContext.Assets.AsNoTracking()
                .CountAsync(a => a.HouseholdId == householdId, cancellationToken);
            result["AssetCount"] = new AssetCountData(count);
        }

        if (activeWidgetTypes.Contains(WidgetType.CylinderIndex))
        {
            var cylinders = await dbContext.Engines.AsNoTracking()
                .Join(dbContext.Assets.AsNoTracking(), e => e.AssetId, a => a.Id, (e, a) => new { e, a.HouseholdId })
                .Where(x => x.HouseholdId == householdId
                    && x.e.Status == EngineStatus.Active
                    && x.e.EngineType == EngineType.Ice
                    && x.e.Cylinders != null)
                .SumAsync(x => (int?)x.e.Cylinders, cancellationToken) ?? 0;

            var engineCount = await dbContext.Engines.AsNoTracking()
                .Join(dbContext.Assets.AsNoTracking(), e => e.AssetId, a => a.Id, (e, a) => new { e, a.HouseholdId })
                .CountAsync(x => x.HouseholdId == householdId
                    && x.e.Status == EngineStatus.Active
                    && x.e.EngineType == EngineType.Ice
                    && x.e.Cylinders != null, cancellationToken);

            result["CylinderIndex"] = new CylinderIndexData(cylinders, engineCount);
        }

        if (activeWidgetTypes.Contains(WidgetType.TotalDisplacement))
        {
            var total = await dbContext.Engines.AsNoTracking()
                .Join(dbContext.Assets.AsNoTracking(), e => e.AssetId, a => a.Id, (e, a) => new { e, a.HouseholdId })
                .Where(x => x.HouseholdId == householdId
                    && x.e.Status == EngineStatus.Active
                    && x.e.DisplacementCC != null)
                .SumAsync(x => (decimal?)x.e.DisplacementCC, cancellationToken) ?? 0m;

            var count = await dbContext.Engines.AsNoTracking()
                .Join(dbContext.Assets.AsNoTracking(), e => e.AssetId, a => a.Id, (e, a) => new { e, a.HouseholdId })
                .CountAsync(x => x.HouseholdId == householdId
                    && x.e.Status == EngineStatus.Active
                    && x.e.DisplacementCC != null, cancellationToken);

            result["TotalDisplacement"] = new TotalDisplacementData(total, count);
        }

        if (activeWidgetTypes.Contains(WidgetType.TotalHorsepower))
        {
            var total = await dbContext.Engines.AsNoTracking()
                .Join(dbContext.Assets.AsNoTracking(), e => e.AssetId, a => a.Id, (e, a) => new { e, a.HouseholdId })
                .Where(x => x.HouseholdId == householdId
                    && x.e.Status == EngineStatus.Active
                    && x.e.HorsepowerHp != null)
                .SumAsync(x => (decimal?)x.e.HorsepowerHp, cancellationToken) ?? 0m;

            var count = await dbContext.Engines.AsNoTracking()
                .Join(dbContext.Assets.AsNoTracking(), e => e.AssetId, a => a.Id, (e, a) => new { e, a.HouseholdId })
                .CountAsync(x => x.HouseholdId == householdId
                    && x.e.Status == EngineStatus.Active
                    && x.e.HorsepowerHp != null, cancellationToken);

            result["TotalHorsepower"] = new TotalHorsepowerData(total, count);
        }

        if (activeWidgetTypes.Contains(WidgetType.TotalTorque))
        {
            var total = await dbContext.Engines.AsNoTracking()
                .Join(dbContext.Assets.AsNoTracking(), e => e.AssetId, a => a.Id, (e, a) => new { e, a.HouseholdId })
                .Where(x => x.HouseholdId == householdId
                    && x.e.Status == EngineStatus.Active
                    && x.e.TorqueNm != null)
                .SumAsync(x => (decimal?)x.e.TorqueNm, cancellationToken) ?? 0m;

            var count = await dbContext.Engines.AsNoTracking()
                .Join(dbContext.Assets.AsNoTracking(), e => e.AssetId, a => a.Id, (e, a) => new { e, a.HouseholdId })
                .CountAsync(x => x.HouseholdId == householdId
                    && x.e.Status == EngineStatus.Active
                    && x.e.TorqueNm != null, cancellationToken);

            result["TotalTorque"] = new TotalTorqueData(total, count);
        }

        if (activeWidgetTypes.Contains(WidgetType.DueSoon))
        {
            var widget = widgetTypes.First(w => w.WidgetType == WidgetType.DueSoon);
            var daysAhead = ParseDueSoonConfig(widget.Config);

            var cutoff = today.AddDays(daysAhead);

            var registrationItems = await dbContext.Registrations.AsNoTracking()
                .Join(dbContext.Assets.AsNoTracking(), r => r.AssetId, a => a.Id, (r, a) => new { r, a })
                .Where(x => x.a.HouseholdId == householdId
                    && x.r.ExpiresOn != null
                    && x.r.ExpiresOn <= cutoff)
                .Select(x => new DueItem(
                    x.a.Id,
                    x.a.Name,
                    "Registration",
                    x.r.ExpiresOn!.Value,
                    ClassifyUrgency(x.r.ExpiresOn!.Value, today)))
                .ToListAsync(cancellationToken);

            var warrantyItems = await dbContext.Warranties.AsNoTracking()
                .Join(dbContext.Assets.AsNoTracking(), w => w.AssetId, a => a.Id, (w, a) => new { w, a })
                .Where(x => x.a.HouseholdId == householdId
                    && x.w.ExpiresOn != null
                    && x.w.ExpiresOn <= cutoff)
                .Select(x => new DueItem(
                    x.a.Id,
                    x.a.Name,
                    "Warranty",
                    x.w.ExpiresOn!.Value,
                    ClassifyUrgency(x.w.ExpiresOn!.Value, today)))
                .ToListAsync(cancellationToken);

            var allItems = registrationItems
                .Concat(warrantyItems)
                .OrderBy(i => i.ExpiresOn)
                .ToList();

            result["DueSoon"] = new DueSoonData(allItems);
        }

        if (activeWidgetTypes.Contains(WidgetType.RecentActivity))
        {
            var widget = widgetTypes.First(w => w.WidgetType == WidgetType.RecentActivity);
            var limit = ParseRecentActivityConfig(widget.Config);

            var items = await dbContext.ServiceRecords.AsNoTracking()
                .Join(dbContext.Assets.AsNoTracking(), sr => sr.AssetId, a => a.Id, (sr, a) => new { sr, a })
                .Where(x => x.a.HouseholdId == householdId)
                .OrderByDescending(x => x.sr.Date)
                .Take(limit)
                .Select(x => new ActivityItem(
                    x.a.Id,
                    x.a.Name,
                    x.sr.Description,
                    x.sr.Date,
                    x.sr.Cost))
                .ToListAsync(cancellationToken);

            result["RecentActivity"] = new RecentActivityData(items);
        }

        if (activeWidgetTypes.Contains(WidgetType.FuelCostYtd))
        {
            var currentYear = DateTime.UtcNow.Year;
            var yearStart = new DateOnly(currentYear, 1, 1);
            var yearEnd = new DateOnly(currentYear, 12, 31);

            var total = await dbContext.FuelLogs.AsNoTracking()
                .Join(dbContext.Assets.AsNoTracking(), fl => fl.AssetId, a => a.Id, (fl, a) => new { fl, a.HouseholdId })
                .Where(x => x.HouseholdId == householdId
                    && x.fl.Date >= yearStart
                    && x.fl.Date <= yearEnd
                    && x.fl.TotalCost != null)
                .SumAsync(x => (decimal?)x.fl.TotalCost, cancellationToken) ?? 0m;

            var logCount = await dbContext.FuelLogs.AsNoTracking()
                .Join(dbContext.Assets.AsNoTracking(), fl => fl.AssetId, a => a.Id, (fl, a) => new { fl, a.HouseholdId })
                .CountAsync(x => x.HouseholdId == householdId
                    && x.fl.Date >= yearStart
                    && x.fl.Date <= yearEnd, cancellationToken);

            result["FuelCostYtd"] = new FuelCostYtdData(total, logCount);
        }

        if (activeWidgetTypes.Contains(WidgetType.MileageMtd))
        {
            var now = DateTime.UtcNow;
            var firstOfMonth = new DateOnly(now.Year, now.Month, 1);

            var total = await dbContext.MileageLogs.AsNoTracking()
                .Join(dbContext.Assets.AsNoTracking(), ml => ml.AssetId, a => a.Id, (ml, a) => new { ml, a.HouseholdId })
                .Where(x => x.HouseholdId == householdId && x.ml.Date >= firstOfMonth)
                .SumAsync(x => (decimal?)(x.ml.TripMiles ?? x.ml.OdometerReading), cancellationToken) ?? 0m;

            var logCount = await dbContext.MileageLogs.AsNoTracking()
                .Join(dbContext.Assets.AsNoTracking(), ml => ml.AssetId, a => a.Id, (ml, a) => new { ml, a.HouseholdId })
                .CountAsync(x => x.HouseholdId == householdId && x.ml.Date >= firstOfMonth, cancellationToken);

            result["MileageMtd"] = new MileageMtdData(total, logCount);
        }

        return result;
    }

    private async Task<DashboardSummaryResponse> SeedDefaultDashboardAsync(
        Guid householdId, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var dashboard = new HouseholdDashboard
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            Name = "Overview",
            IsDefault = true,
            Position = 0,
            Widgets =
            [
                new DashboardWidget { Id = Guid.NewGuid(), WidgetType = WidgetType.CylinderIndex, WidgetSize = WidgetSize.Small, Position = 0 },
                new DashboardWidget { Id = Guid.NewGuid(), WidgetType = WidgetType.TotalDisplacement, WidgetSize = WidgetSize.Small, Position = 1 },
                new DashboardWidget { Id = Guid.NewGuid(), WidgetType = WidgetType.TotalHorsepower, WidgetSize = WidgetSize.Small, Position = 2 },
                new DashboardWidget { Id = Guid.NewGuid(), WidgetType = WidgetType.AssetCount, WidgetSize = WidgetSize.Small, Position = 3 },
                new DashboardWidget { Id = Guid.NewGuid(), WidgetType = WidgetType.RecentActivity, WidgetSize = WidgetSize.Full, Position = 4, Config = "{\"limit\":5}" },
                new DashboardWidget { Id = Guid.NewGuid(), WidgetType = WidgetType.DueSoon, WidgetSize = WidgetSize.Full, Position = 5, Config = "{\"daysAhead\":30}" },
            ],
        };

        dbContext.HouseholdDashboards.Add(dashboard);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new DashboardSummaryResponse(dashboard.Id, dashboard.Name, dashboard.IsDefault, dashboard.Position);
    }

    private async Task DemoteCurrentDefaultAsync(
        Guid householdId, Guid? excludeId, CancellationToken cancellationToken)
    {
        var current = await dbContext.HouseholdDashboards
            .Where(d => d.HouseholdId == householdId && d.IsDefault && d.Id != excludeId)
            .FirstOrDefaultAsync(cancellationToken);

        if (current is not null)
        {
            current.IsDefault = false;
        }
    }

    private async Task<int> GetNextPositionAsync(Guid householdId, CancellationToken cancellationToken)
    {
        var max = await dbContext.HouseholdDashboards.AsNoTracking()
            .Where(d => d.HouseholdId == householdId)
            .MaxAsync(d => (int?)d.Position, cancellationToken);

        return (max ?? -1) + 1;
    }

    private static int ParseDueSoonConfig(string? config)
    {
        if (config is null) return 30;
        try
        {
            var doc = JsonDocument.Parse(config);
            if (doc.RootElement.TryGetProperty("daysAhead", out var prop))
                return prop.GetInt32();
        }
        catch { }
        return 30;
    }

    private static int ParseRecentActivityConfig(string? config)
    {
        if (config is null) return 5;
        try
        {
            var doc = JsonDocument.Parse(config);
            if (doc.RootElement.TryGetProperty("limit", out var prop))
                return Math.Min(prop.GetInt32(), 20);
        }
        catch { }
        return 5;
    }

    private static string ClassifyUrgency(DateOnly expiresOn, DateOnly today)
    {
        if (expiresOn < today) return "Overdue";
        if (expiresOn <= today.AddDays(7)) return "DueSoon";
        return "Upcoming";
    }
}
