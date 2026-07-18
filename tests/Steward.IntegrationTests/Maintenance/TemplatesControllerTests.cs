using System.Net;
using System.Net.Http.Json;
using Steward.Application.Tracking.Templates;
using Steward.Domain.Enums;
using Steward.IntegrationTests.Infrastructure;

namespace Steward.IntegrationTests.Maintenance;

public class TemplatesControllerTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Contributor_Creates_Household_Template()
    {
        var ct = TestContext.Current.CancellationToken;
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/templates",
            new CreateTemplateRequest("Spring commissioning", null, [AssetCategory.PowerBoat, AssetCategory.Sailboat]),
            TestJson.Options, ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TemplateResponse>(TestJson.Options, ct);
        Assert.NotNull(created!.HouseholdId);
        Assert.Equal(householdId, created.HouseholdId);
    }

    [Fact]
    public async Task Viewer_Cannot_Create_Household_Template()
    {
        var ct = TestContext.Current.CancellationToken;
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Viewer);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/templates",
            new CreateTemplateRequest("Spring commissioning", null, null),
            TestJson.Options, ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task List_Household_Templates_Filters_By_Asset_Category()
    {
        var ct = TestContext.Current.CancellationToken;
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);

        await client.PostAsJsonAsync(
            $"/api/households/{householdId}/templates",
            new CreateTemplateRequest("Snowmobile tuneup", null, [AssetCategory.Snowmobile]),
            TestJson.Options, ct);
        await client.PostAsJsonAsync(
            $"/api/households/{householdId}/templates",
            new CreateTemplateRequest("Boat commissioning", null, [AssetCategory.PowerBoat]),
            TestJson.Options, ct);

        var response = await client.GetAsync($"/api/households/{householdId}/templates?assetCategory=Snowmobile", ct);
        var templates = await response.Content.ReadFromJsonAsync<List<TemplateResponse>>(TestJson.Options, ct);

        Assert.Single(templates!);
        Assert.Equal("Snowmobile tuneup", templates![0].Title);
    }

    [Fact]
    public async Task Deleting_A_Household_Template_Does_Not_Require_Special_Handling_For_Existing_Items()
    {
        var ct = TestContext.Current.CancellationToken;
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);

        var template = await (await client.PostAsJsonAsync(
            $"/api/households/{householdId}/templates",
            new CreateTemplateRequest("Oil change", null, null),
            TestJson.Options, ct)).Content.ReadFromJsonAsync<TemplateResponse>(TestJson.Options, ct);

        var assetId = await CreateAssetAsync(householdId);
        var itemResponse = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items",
            new Steward.Application.Tracking.MaintenanceItems.CreateMaintenanceItemRequest(
                "Oil change", null, null, null, null, null, null, null, null, template!.Id),
            TestJson.Options, ct);
        var item = await itemResponse.Content.ReadFromJsonAsync<Steward.Application.Tracking.MaintenanceItems.MaintenanceItemResponse>(TestJson.Options, ct);

        var deleteResponse = await client.DeleteAsync($"/api/households/{householdId}/templates/{template.Id}", ct);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getItemResponse = await client.GetAsync(
            $"/api/households/{householdId}/assets/{assetId}/maintenance-items/{item!.Id}", ct);
        Assert.Equal(HttpStatusCode.OK, getItemResponse.StatusCode);
        var fetchedItem = await getItemResponse.Content.ReadFromJsonAsync<Steward.Application.Tracking.MaintenanceItems.MaintenanceItemResponse>(TestJson.Options, ct);
        Assert.Equal(template.Id, fetchedItem!.TemplateId);
    }

    [Fact]
    public async Task Any_Authenticated_User_Can_Read_Platform_Catalog()
    {
        var ct = TestContext.Current.CancellationToken;
        var (_, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Viewer);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.GetAsync("/api/templates/platform", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var templates = await response.Content.ReadFromJsonAsync<List<TemplateResponse>>(TestJson.Options, ct);
        Assert.NotEmpty(templates!);
        Assert.All(templates!, t => Assert.Null(t.HouseholdId));
    }

    [Fact]
    public async Task PlatformAdmin_Creates_Platform_Template()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = CreateAuthenticatedClient(Guid.NewGuid(), roles: ["PlatformAdmin"]);

        var response = await client.PostAsJsonAsync(
            "/api/admin/templates", new CreateTemplateRequest("Custom oil change", null, null), TestJson.Options, ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TemplateResponse>(TestJson.Options, ct);
        Assert.Null(created!.HouseholdId);
    }

    [Fact]
    public async Task NonAdmin_Cannot_Create_Platform_Template()
    {
        var ct = TestContext.Current.CancellationToken;
        var (_, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync(
            "/api/admin/templates", new CreateTemplateRequest("Custom oil change", null, null), TestJson.Options, ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NonAdmin_Cannot_Edit_Or_Delete_A_Platform_Template()
    {
        var ct = TestContext.Current.CancellationToken;
        var adminClient = CreateAuthenticatedClient(Guid.NewGuid(), roles: ["PlatformAdmin"]);
        var template = await (await adminClient.PostAsJsonAsync(
            "/api/admin/templates", new CreateTemplateRequest("Custom oil change", null, null), TestJson.Options, ct))
            .Content.ReadFromJsonAsync<TemplateResponse>(TestJson.Options, ct);

        var (_, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);

        var patchResponse = await client.PatchAsJsonAsync(
            $"/api/admin/templates/{template!.Id}", new { title = "Hacked" }, TestJson.Options, ct);
        Assert.Equal(HttpStatusCode.Forbidden, patchResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync($"/api/admin/templates/{template.Id}", ct);
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Duplicating_A_Platform_Template_Creates_An_Independent_Copy()
    {
        var ct = TestContext.Current.CancellationToken;
        var adminClient = CreateAuthenticatedClient(Guid.NewGuid(), roles: ["PlatformAdmin"]);
        var platformTemplate = await (await adminClient.PostAsJsonAsync(
            "/api/admin/templates", new CreateTemplateRequest("Oil change", null, null), TestJson.Options, ct))
            .Content.ReadFromJsonAsync<TemplateResponse>(TestJson.Options, ct);

        await adminClient.PostAsJsonAsync(
            $"/api/admin/templates/{platformTemplate!.Id}/steps",
            new CreateTemplateStepRequest("Change oil", false, null, null, null, [new SuggestedPartDto("Oil filter", 1)]),
            TestJson.Options, ct);

        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var client = CreateAuthenticatedClient(userId);

        var duplicateResponse = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/templates/duplicate",
            new DuplicateTemplateRequest(platformTemplate.Id), TestJson.Options, ct);

        Assert.Equal(HttpStatusCode.Created, duplicateResponse.StatusCode);
        var copy = await duplicateResponse.Content.ReadFromJsonAsync<TemplateResponse>(TestJson.Options, ct);
        Assert.Equal(householdId, copy!.HouseholdId);
        Assert.Single(copy.Steps);
        Assert.NotEqual(platformTemplate.Steps.Count > 0 ? platformTemplate.Steps[0].Id : Guid.Empty, copy.Steps[0].Id);

        await client.PatchAsJsonAsync(
            $"/api/households/{householdId}/templates/{copy.Id}", new { title = "My custom oil change" }, TestJson.Options, ct);

        var originalAfterEdit = await (await client.GetAsync("/api/templates/platform", ct))
            .Content.ReadFromJsonAsync<List<TemplateResponse>>(TestJson.Options, ct);
        Assert.Contains(originalAfterEdit!, t => t.Id == platformTemplate.Id && t.Title == "Oil change");
    }

    [Fact]
    public async Task PlatformTemplateId_Must_Reference_A_Platform_Template()
    {
        var ct = TestContext.Current.CancellationToken;
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var client = CreateAuthenticatedClient(userId);

        var householdTemplate = await (await client.PostAsJsonAsync(
            $"/api/households/{householdId}/templates", new CreateTemplateRequest("Mine", null, null), TestJson.Options, ct))
            .Content.ReadFromJsonAsync<TemplateResponse>(TestJson.Options, ct);

        var response = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/templates/duplicate",
            new DuplicateTemplateRequest(householdTemplate!.Id), TestJson.Options, ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
