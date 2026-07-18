using Steward.Application.Tracking.MaintenanceItems;
using Steward.Domain.Common.Exceptions;
using Steward.Domain.Entities;
using Steward.Domain.Enums;
using Steward.Infrastructure.Maintenance;
using Steward.Infrastructure.Persistence;
using Steward.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Steward.IntegrationTests.Maintenance;

/// Exercises MaintenanceItemService's template-expansion logic directly against a real
/// Postgres schema (Template.ApplicableCategories, a native array column, isn't
/// model-validatable under EF Core's InMemory provider, so this can't live in UnitTests).
/// isBlocked derivation (which never touches Template) lives in Steward.UnitTests instead.
public class MaintenanceItemServiceLogicTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    private async Task<(StewardDbContext DbContext, MaintenanceItemService Service, Guid AssetId)> CreateContextAsync(
        AssetCategory category)
    {
        var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StewardDbContext>();
        var householdId = await CreateHouseholdAsync();
        var assetId = await CreateAssetOfCategoryAsync(householdId, category);
        return (dbContext, new MaintenanceItemService(dbContext), assetId);
    }

    private async Task<Guid> CreateAssetOfCategoryAsync(Guid householdId, AssetCategory category)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StewardDbContext>();

        var now = DateTimeOffset.UtcNow;
        var asset = new Steward.Domain.Entities.Assets.Boat
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            Category = category,
            Name = "Test Boat",
            UsageTrackingMode = UsageTrackingMode.Hours,
            CreatedAt = now,
            UpdatedAt = now,
        };
        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync();
        return asset.Id;
    }

    [Fact]
    public async Task Create_From_Template_Expands_EngineScoped_Step_Per_Active_Engine()
    {
        var ct = TestContext.Current.CancellationToken;
        var (dbContext, service, assetId) = await CreateContextAsync(AssetCategory.PowerBoat);
        await CreateEngineAsync(assetId, "Port", status: EngineStatus.Active);
        await CreateEngineAsync(assetId, "Starboard", status: EngineStatus.Active);

        var template = new Template
        {
            Id = Guid.NewGuid(),
            HouseholdId = null,
            Title = $"Winterize {Guid.NewGuid()}",
            ApplicableCategories = [AssetCategory.PowerBoat],
            Steps = [new TemplateStep { Id = Guid.NewGuid(), Text = "Fog cylinders", SortOrder = 0, EngineScoped = true }],
        };
        dbContext.Templates.Add(template);
        await dbContext.SaveChangesAsync(ct);

        var response = await service.CreateAsync(
            assetId,
            new CreateMaintenanceItemRequest("Winterize the boat", null, null, null, null, null, null, null, null, template.Id),
            ct);

        Assert.Equal(2, response.ChecklistItems.Count);
        Assert.All(response.ChecklistItems, c => Assert.Equal(template.Steps[0].Id, c.TemplateStepId));
        Assert.Equal(2, response.ChecklistItems.Select(c => c.EngineId).Distinct().Count());
    }

    [Fact]
    public async Task Create_From_Template_Excludes_Retired_Engines()
    {
        var ct = TestContext.Current.CancellationToken;
        var (dbContext, service, assetId) = await CreateContextAsync(AssetCategory.PowerBoat);
        await CreateEngineAsync(assetId, "Main", status: EngineStatus.Active);
        await CreateEngineAsync(assetId, "Old", status: EngineStatus.Retired);

        var template = new Template
        {
            Id = Guid.NewGuid(),
            HouseholdId = null,
            Title = $"Oil change {Guid.NewGuid()}",
            ApplicableCategories = [],
            Steps = [new TemplateStep { Id = Guid.NewGuid(), Text = "Change oil", SortOrder = 0, EngineScoped = true }],
        };
        dbContext.Templates.Add(template);
        await dbContext.SaveChangesAsync(ct);

        var response = await service.CreateAsync(
            assetId,
            new CreateMaintenanceItemRequest("Oil change", null, null, null, null, null, null, null, null, template.Id),
            ct);

        Assert.Single(response.ChecklistItems);
    }

    [Fact]
    public async Task Create_From_Template_Suggested_Parts_Are_Copied_To_PartLines()
    {
        var ct = TestContext.Current.CancellationToken;
        var (dbContext, service, assetId) = await CreateContextAsync(AssetCategory.PowerBoat);

        var template = new Template
        {
            Id = Guid.NewGuid(),
            HouseholdId = null,
            Title = $"Oil change {Guid.NewGuid()}",
            ApplicableCategories = [],
            Steps =
            [
                new TemplateStep
                {
                    Id = Guid.NewGuid(),
                    Text = "Change oil",
                    SortOrder = 0,
                    EngineScoped = false,
                    SuggestedParts = [new TemplateStepSuggestedPart { Id = Guid.NewGuid(), Name = "Oil filter", Quantity = 1 }],
                },
            ],
        };
        dbContext.Templates.Add(template);
        await dbContext.SaveChangesAsync(ct);

        var response = await service.CreateAsync(
            assetId,
            new CreateMaintenanceItemRequest("Oil change", null, null, null, null, null, null, null, null, template.Id),
            ct);

        var partLine = Assert.Single(response.PartLines);
        Assert.Equal("Oil filter", partLine.Name);
        Assert.Equal(PartLineStatus.Needed, partLine.Status);
        Assert.Equal(response.ChecklistItems[0].Id, partLine.ChecklistItemId);
    }

    [Fact]
    public async Task Create_From_Template_Rejects_Category_Mismatch()
    {
        var ct = TestContext.Current.CancellationToken;
        var (dbContext, service, assetId) = await CreateContextAsync(AssetCategory.Car);

        var template = new Template
        {
            Id = Guid.NewGuid(),
            HouseholdId = null,
            Title = $"Winterize {Guid.NewGuid()}",
            ApplicableCategories = [AssetCategory.PowerBoat, AssetCategory.Sailboat],
        };
        dbContext.Templates.Add(template);
        await dbContext.SaveChangesAsync(ct);

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(
            assetId,
            new CreateMaintenanceItemRequest("Winterize", null, null, null, null, null, null, null, null, template.Id),
            ct));
    }
}
