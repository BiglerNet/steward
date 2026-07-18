using System.Net;
using System.Net.Http.Json;
using Steward.Application.Tracking.MaintenanceItems;
using Steward.Domain.Enums;
using Steward.IntegrationTests.Infrastructure;

namespace Steward.IntegrationTests.Maintenance;

public class MaintenanceItemsControllerTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Contributor_Can_Create_List_Get_Patch_And_Delete_Maintenance_Item()
    {
        var ct = TestContext.Current.CancellationToken;
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var createResponse = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items",
            new CreateMaintenanceItemRequest("Oil change", null, null, null, null, null, null, null, null, null),
            TestJson.Options, ct);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<MaintenanceItemResponse>(TestJson.Options, ct);
        Assert.NotNull(created);
        Assert.Equal(MaintenanceItemStatus.Planned, created!.Status);

        var listResponse = await client.GetAsync($"/api/households/{householdId}/assets/{assetId}/maintenance-items", ct);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<List<MaintenanceItemResponse>>(TestJson.Options, ct);
        Assert.Single(list!);

        var getResponse = await client.GetAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var patchBody = new { title = "Oil change (synthetic)" };
        var patchResponse = await client.PatchAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items/{created.Id}", patchBody, TestJson.Options, ct);
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
        var patched = await patchResponse.Content.ReadFromJsonAsync<MaintenanceItemResponse>(TestJson.Options, ct);
        Assert.Equal("Oil change (synthetic)", patched!.Title);

        var deleteResponse = await client.DeleteAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items/{created.Id}", ct);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Quick_Logging_A_Completed_Entry_In_One_Request()
    {
        var ct = TestContext.Current.CancellationToken;
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items",
            new CreateMaintenanceItemRequest(
                "Oil change", null, null, MaintenanceItemStatus.Done, new DateOnly(2026, 6, 1), 85.00m, null, null, null, null),
            TestJson.Options, ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<MaintenanceItemResponse>(TestJson.Options, ct);
        Assert.Equal(MaintenanceItemStatus.Done, created!.Status);
        Assert.Empty(created.ChecklistItems);
        Assert.Empty(created.PartLines);
    }

    [Fact]
    public async Task Viewer_Cannot_Create_Maintenance_Item()
    {
        var ct = TestContext.Current.CancellationToken;
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Viewer);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items",
            new CreateMaintenanceItemRequest("Oil change", null, null, null, null, null, null, null, null, null),
            TestJson.Options, ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_Can_List_Maintenance_Items()
    {
        var ct = TestContext.Current.CancellationToken;
        var (householdId, ownerId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var viewerId = Guid.NewGuid();
        await AddMemberAsync(householdId, viewerId, HouseholdMemberRole.Viewer);

        var ownerClient = CreateAuthenticatedClient(ownerId);
        await ownerClient.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items",
            new CreateMaintenanceItemRequest("Oil change", null, null, null, null, null, null, null, null, null),
            TestJson.Options, ct);

        var viewerClient = CreateAuthenticatedClient(viewerId);
        var response = await viewerClient.GetAsync($"/api/households/{householdId}/assets/{assetId}/maintenance-items", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task NonMember_Cannot_Access_Maintenance_Items()
    {
        var ct = TestContext.Current.CancellationToken;
        var (householdId, _) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var nonMemberClient = CreateAuthenticatedClient(Guid.NewGuid());

        var response = await nonMemberClient.GetAsync($"/api/households/{householdId}/assets/{assetId}/maintenance-items", ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Missing_Title_Rejected()
    {
        var ct = TestContext.Current.CancellationToken;
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items",
            new CreateMaintenanceItemRequest("", null, null, null, null, null, null, null, null, null),
            TestJson.Options, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EngineId_From_Different_Asset_Rejected()
    {
        var ct = TestContext.Current.CancellationToken;
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var otherAssetId = await CreateAssetAsync(householdId, "Other Asset");
        var otherEngineId = await CreateEngineAsync(otherAssetId);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items",
            new CreateMaintenanceItemRequest("Oil change", null, null, null, null, null, null, null, otherEngineId, null),
            TestJson.Options, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Completing_An_Item_With_Open_Checklist_Items_Is_Allowed()
    {
        var ct = TestContext.Current.CancellationToken;
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var created = await (await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items",
            new CreateMaintenanceItemRequest("Rebuild", null, null, null, null, null, null, null, null, null),
            TestJson.Options, ct)).Content.ReadFromJsonAsync<MaintenanceItemResponse>(TestJson.Options, ct);

        await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items/{created!.Id}/checklist-items",
            new CreateChecklistItemRequest("Step 1", null), TestJson.Options, ct);

        var patchResponse = await client.PatchAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items/{created.Id}",
            new { status = "Done" }, TestJson.Options, ct);

        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
        var updated = await patchResponse.Content.ReadFromJsonAsync<MaintenanceItemResponse>(TestJson.Options, ct);
        Assert.Equal(MaintenanceItemStatus.Done, updated!.Status);
        Assert.Single(updated.ChecklistItems);
        Assert.Equal(ChecklistItemStatus.Open, updated.ChecklistItems[0].Status);
    }

    [Fact]
    public async Task Checklist_Item_Checked_Off_Sets_ResolvedAt_And_Reopen_Clears_It()
    {
        var ct = TestContext.Current.CancellationToken;
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var created = await (await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items",
            new CreateMaintenanceItemRequest("Rebuild", null, null, null, null, null, null, null, null, null),
            TestJson.Options, ct)).Content.ReadFromJsonAsync<MaintenanceItemResponse>(TestJson.Options, ct);

        var checklistItem = await (await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items/{created!.Id}/checklist-items",
            new CreateChecklistItemRequest("Check trailer lights", null), TestJson.Options, ct))
            .Content.ReadFromJsonAsync<ChecklistItemResponse>(TestJson.Options, ct);

        var doneResponse = await client.PatchAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items/{created.Id}/checklist-items/{checklistItem!.Id}",
            new { status = "Done" }, TestJson.Options, ct);
        var done = await doneResponse.Content.ReadFromJsonAsync<ChecklistItemResponse>(TestJson.Options, ct);
        Assert.Equal(ChecklistItemStatus.Done, done!.Status);
        Assert.NotNull(done.ResolvedAt);

        var reopenResponse = await client.PatchAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items/{created.Id}/checklist-items/{checklistItem.Id}",
            new { status = "Open" }, TestJson.Options, ct);
        var reopened = await reopenResponse.Content.ReadFromJsonAsync<ChecklistItemResponse>(TestJson.Options, ct);
        Assert.Equal(ChecklistItemStatus.Open, reopened!.Status);
        Assert.Null(reopened.ResolvedAt);
    }

    [Fact]
    public async Task Reorder_Checklist_Items_Persists_New_Order()
    {
        var ct = TestContext.Current.CancellationToken;
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var created = await (await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items",
            new CreateMaintenanceItemRequest("Rebuild", null, null, null, null, null, null, null, null, null),
            TestJson.Options, ct)).Content.ReadFromJsonAsync<MaintenanceItemResponse>(TestJson.Options, ct);

        var first = await (await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items/{created!.Id}/checklist-items",
            new CreateChecklistItemRequest("First", null), TestJson.Options, ct))
            .Content.ReadFromJsonAsync<ChecklistItemResponse>(TestJson.Options, ct);
        var second = await (await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items/{created.Id}/checklist-items",
            new CreateChecklistItemRequest("Second", null), TestJson.Options, ct))
            .Content.ReadFromJsonAsync<ChecklistItemResponse>(TestJson.Options, ct);

        var reorderResponse = await client.PutAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items/{created.Id}/checklist-items/reorder",
            new ReorderChecklistItemsRequest([second!.Id, first!.Id]), TestJson.Options, ct);

        Assert.Equal(HttpStatusCode.OK, reorderResponse.StatusCode);
        var reordered = await reorderResponse.Content.ReadFromJsonAsync<List<ChecklistItemResponse>>(TestJson.Options, ct);
        Assert.Equal(second.Id, reordered![0].Id);
        Assert.Equal(first.Id, reordered[1].Id);
    }

    [Fact]
    public async Task Reorder_Missing_An_Existing_Item_Id_Is_Rejected()
    {
        var ct = TestContext.Current.CancellationToken;
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var created = await (await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items",
            new CreateMaintenanceItemRequest("Rebuild", null, null, null, null, null, null, null, null, null),
            TestJson.Options, ct)).Content.ReadFromJsonAsync<MaintenanceItemResponse>(TestJson.Options, ct);

        await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items/{created!.Id}/checklist-items",
            new CreateChecklistItemRequest("First", null), TestJson.Options, ct);
        await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items/{created.Id}/checklist-items",
            new CreateChecklistItemRequest("Second", null), TestJson.Options, ct);

        var reorderResponse = await client.PutAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items/{created.Id}/checklist-items/reorder",
            new ReorderChecklistItemsRequest([Guid.NewGuid()]), TestJson.Options, ct);

        Assert.Equal(HttpStatusCode.BadRequest, reorderResponse.StatusCode);
    }

    [Fact]
    public async Task Part_Line_Blocked_Badge_Clears_When_Received()
    {
        var ct = TestContext.Current.CancellationToken;
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var created = await (await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items",
            new CreateMaintenanceItemRequest("Brake job", null, null, null, null, null, null, null, null, null),
            TestJson.Options, ct)).Content.ReadFromJsonAsync<MaintenanceItemResponse>(TestJson.Options, ct);

        var partLine = await (await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items/{created!.Id}/part-lines",
            new CreatePartLineRequest("Brake pads", null, null, null, null, 1, null, null), TestJson.Options, ct))
            .Content.ReadFromJsonAsync<PartLineResponse>(TestJson.Options, ct);

        var afterCreate = await (await client.GetAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items/{created.Id}", ct))
            .Content.ReadFromJsonAsync<MaintenanceItemResponse>(TestJson.Options, ct);
        Assert.True(afterCreate!.IsBlocked);

        await client.PatchAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items/{created.Id}/part-lines/{partLine!.Id}",
            new { status = "Received" }, TestJson.Options, ct);

        var afterReceived = await (await client.GetAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items/{created.Id}", ct))
            .Content.ReadFromJsonAsync<MaintenanceItemResponse>(TestJson.Options, ct);
        Assert.False(afterReceived!.IsBlocked);
    }

    [Fact]
    public async Task Maintenance_Item_Under_Different_Household_Asset_Returns_NotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var (householdA, _) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdA);

        var (householdB, userB) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userB);

        var response = await client.GetAsync($"/api/households/{householdB}/assets/{assetId}/maintenance-items", ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
