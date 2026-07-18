using System.Net;
using System.Net.Http.Json;
using Steward.Application.Tracking.MaintenanceItems;
using Steward.Application.Tracking.MaintenanceRecurrence;
using Steward.Domain.Enums;
using Steward.IntegrationTests.Infrastructure;

namespace Steward.IntegrationTests.Maintenance;

public class MaintenanceScheduleControllerTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Active_Member_Can_Read_Schedule()
    {
        var ct = TestContext.Current.CancellationToken;
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Viewer);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.GetAsync($"/api/households/{householdId}/assets/{assetId}/maintenance-schedule", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PlatformAdmin_Can_Read_Schedule()
    {
        var ct = TestContext.Current.CancellationToken;
        var (householdId, _) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var adminClient = CreateAuthenticatedClient(Guid.NewGuid(), roles: ["PlatformAdmin"]);

        var response = await adminClient.GetAsync($"/api/households/{householdId}/assets/{assetId}/maintenance-schedule", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task NonMember_Cannot_Read_Schedule()
    {
        var ct = TestContext.Current.CancellationToken;
        var (householdId, _) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var nonMemberClient = CreateAuthenticatedClient(Guid.NewGuid());

        var response = await nonMemberClient.GetAsync($"/api/households/{householdId}/assets/{assetId}/maintenance-schedule", ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Never_Done_Step_Appears_With_Null_LastDoneAt()
    {
        var ct = TestContext.Current.CancellationToken;
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var (templateId, stepId) = await CreateTemplateStepAsync();
        var client = CreateAuthenticatedClient(userId);

        var created = await (await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items",
            new CreateMaintenanceItemRequest("Winterize", null, null, null, null, null, null, null, null, templateId),
            TestJson.Options, ct)).Content.ReadFromJsonAsync<MaintenanceItemResponse>(TestJson.Options, ct);

        var checklistItem = created!.ChecklistItems.Single(c => c.TemplateStepId == stepId);
        await client.PatchAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items/{created.Id}/checklist-items/{checklistItem.Id}",
            new { status = "Skipped" }, TestJson.Options, ct);

        var response = await client.GetAsync($"/api/households/{householdId}/assets/{assetId}/maintenance-schedule", ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var schedule = await response.Content.ReadFromJsonAsync<List<MaintenanceScheduleEntryResponse>>(TestJson.Options, ct);

        var entry = Assert.Single(schedule!);
        Assert.Equal(stepId, entry.TemplateStepId);
        Assert.Null(entry.LastDoneAt);
    }

    [Fact]
    public async Task Independent_Engines_Produce_Independent_Schedule_Entries()
    {
        var ct = TestContext.Current.CancellationToken;
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var mainEngineId = await CreateEngineAsync(assetId, "Main");
        var kickerEngineId = await CreateEngineAsync(assetId, "Kicker");
        var (templateId, stepId) = await CreateTemplateStepAsync(engineScoped: true);
        var client = CreateAuthenticatedClient(userId);

        var created = await (await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items",
            new CreateMaintenanceItemRequest("Change oil", null, null, null, null, null, null, null, null, templateId),
            TestJson.Options, ct)).Content.ReadFromJsonAsync<MaintenanceItemResponse>(TestJson.Options, ct);

        Assert.Equal(2, created!.ChecklistItems.Count);
        var mainChecklistItem = created.ChecklistItems.Single(c => c.EngineId == mainEngineId);
        await client.PatchAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items/{created.Id}/checklist-items/{mainChecklistItem.Id}",
            new { status = "Done" }, TestJson.Options, ct);

        var response = await client.GetAsync($"/api/households/{householdId}/assets/{assetId}/maintenance-schedule", ct);
        var schedule = await response.Content.ReadFromJsonAsync<List<MaintenanceScheduleEntryResponse>>(TestJson.Options, ct);

        Assert.Equal(2, schedule!.Count);
        var mainEntry = schedule.Single(e => e.EngineId == mainEngineId);
        var kickerEntry = schedule.Single(e => e.EngineId == kickerEngineId);
        Assert.NotNull(mainEntry.LastDoneAt);
        Assert.Null(kickerEntry.LastDoneAt);
        Assert.Equal(stepId, mainEntry.TemplateStepId);
    }
}
