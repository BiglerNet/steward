using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Steward.Application.Dashboards;
using Steward.Domain.Entities;
using Steward.Domain.Enums;
using Steward.Infrastructure.Persistence;
using Steward.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Steward.IntegrationTests.Dashboards;

public class DashboardsControllerTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    // ─── List / Auto-seed ────────────────────────────────────────────────────

    [Fact]
    public async Task List_Dashboards_On_Empty_Household_Autoseeds_Overview()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.GetAsync($"/api/households/{householdId}/dashboards", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dashboards = await response.Content.ReadFromJsonAsync<List<DashboardSummaryResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(dashboards);
        Assert.Single(dashboards);
        Assert.Equal("Overview", dashboards[0].Name);
        Assert.True(dashboards[0].IsDefault);
    }

    [Fact]
    public async Task Autoseeded_Overview_Dashboard_Has_Expected_Widget_Composition_And_Order()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);

        var listResponse = await client.GetAsync($"/api/households/{householdId}/dashboards", TestContext.Current.CancellationToken);
        var dashboards = await listResponse.Content.ReadFromJsonAsync<List<DashboardSummaryResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        var dashboardId = dashboards!.Single(d => d.Name == "Overview").Id;

        var detailResponse = await client.GetAsync($"/api/households/{householdId}/dashboards/{dashboardId}", TestContext.Current.CancellationToken);
        var detail = await detailResponse.Content.ReadFromJsonAsync<DashboardDetailResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        var widgets = detail!.Widgets.OrderBy(w => w.Position).ToList();
        Assert.Equal(6, widgets.Count);
        Assert.Equal(
            [WidgetType.CylinderIndex, WidgetType.TotalDisplacement, WidgetType.TotalHorsepower, WidgetType.AssetCount, WidgetType.RecentActivity, WidgetType.DueSoon],
            widgets.Select(w => w.WidgetType));
        Assert.Equal(
            [WidgetSize.Small, WidgetSize.Small, WidgetSize.Small, WidgetSize.Small, WidgetSize.Full, WidgetSize.Full],
            widgets.Select(w => w.WidgetSize));
        Assert.Equal([0, 1, 2, 3, 4, 5], widgets.Select(w => w.Position));
    }

    [Fact]
    public async Task Viewer_Can_List_Dashboards()
    {
        var (householdId, ownerId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var viewerId = Guid.NewGuid();
        await AddMemberAsync(householdId, viewerId, HouseholdMemberRole.Viewer);
        var client = CreateAuthenticatedClient(viewerId);

        var response = await client.GetAsync($"/api/households/{householdId}/dashboards", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task NonMember_Cannot_List_Dashboards()
    {
        var (householdId, _) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var nonMemberId = Guid.NewGuid();
        await CreateUserAsync(nonMemberId);
        var client = CreateAuthenticatedClient(nonMemberId);

        var response = await client.GetAsync($"/api/households/{householdId}/dashboards", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ─── CRUD ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Contributor_Can_Create_Dashboard()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/dashboards", new CreateDashboardRequest("Fuel & Mileage", null), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dashboard = await response.Content.ReadFromJsonAsync<DashboardSummaryResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(dashboard);
        Assert.Equal("Fuel & Mileage", dashboard.Name);
        Assert.False(dashboard.IsDefault);
    }

    [Fact]
    public async Task Create_Dashboard_With_IsDefault_Demotes_Prior_Default()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);

        // Seed default
        await client.GetAsync($"/api/households/{householdId}/dashboards", TestContext.Current.CancellationToken);

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/dashboards", new CreateDashboardRequest("New Default", true), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var newDefault = await response.Content.ReadFromJsonAsync<DashboardSummaryResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(newDefault!.IsDefault);

        var listResponse = await client.GetAsync($"/api/households/{householdId}/dashboards", TestContext.Current.CancellationToken);
        var dashboards = await listResponse.Content.ReadFromJsonAsync<List<DashboardSummaryResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(dashboards);
        Assert.Single(dashboards, d => d.IsDefault);
        Assert.True(dashboards.First(d => d.Name == "New Default").IsDefault);
        Assert.False(dashboards.First(d => d.Name == "Overview").IsDefault);
    }

    [Fact]
    public async Task Duplicate_Dashboard_Name_Returns_BadRequest()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);
        await client.PostAsJsonAsync($"/api/households/{householdId}/dashboards", new CreateDashboardRequest("MyDash", null), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/dashboards", new CreateDashboardRequest("MyDash", null), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_Cannot_Create_Dashboard()
    {
        var (householdId, ownerId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var viewerId = Guid.NewGuid();
        await AddMemberAsync(householdId, viewerId, HouseholdMemberRole.Viewer);
        var client = CreateAuthenticatedClient(viewerId);

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/dashboards", new CreateDashboardRequest("Test", null), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Owner_Can_Rename_Dashboard()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);
        await client.GetAsync($"/api/households/{householdId}/dashboards", TestContext.Current.CancellationToken);
        var dashboards = await (await client.GetAsync($"/api/households/{householdId}/dashboards", TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<List<DashboardSummaryResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        var d = dashboards![0];

        var response = await client.PutAsJsonAsync($"/api/households/{householdId}/dashboards/{d.Id}", new UpdateDashboardRequest("My Overview", true, 0), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<DashboardSummaryResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("My Overview", updated!.Name);
    }

    [Fact]
    public async Task Owner_Can_Delete_Non_Only_Dashboard()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);
        // Seed Overview
        await client.GetAsync($"/api/households/{householdId}/dashboards", TestContext.Current.CancellationToken);
        // Create second
        var second = await (await client.PostAsJsonAsync($"/api/households/{householdId}/dashboards", new CreateDashboardRequest("Second", null), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<DashboardSummaryResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        var response = await client.DeleteAsync($"/api/households/{householdId}/dashboards/{second!.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Last_Dashboard_Returns_BadRequest()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);
        var dashboards = await (await client.GetAsync($"/api/households/{householdId}/dashboards", TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<List<DashboardSummaryResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        var response = await client.DeleteAsync($"/api/households/{householdId}/dashboards/{dashboards![0].Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Contributor_Cannot_Delete_Dashboard()
    {
        var (householdId, ownerId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var ownerClient = CreateAuthenticatedClient(ownerId);
        await ownerClient.GetAsync($"/api/households/{householdId}/dashboards", TestContext.Current.CancellationToken);
        await ownerClient.PostAsJsonAsync($"/api/households/{householdId}/dashboards", new CreateDashboardRequest("Second", null), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        var dashboards = await (await ownerClient.GetAsync($"/api/households/{householdId}/dashboards", TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<List<DashboardSummaryResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        var contributorId = Guid.NewGuid();
        await AddMemberAsync(householdId, contributorId, HouseholdMemberRole.Contributor);
        var contributorClient = CreateAuthenticatedClient(contributorId);

        var response = await contributorClient.DeleteAsync($"/api/households/{householdId}/dashboards/{dashboards![0].Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ─── Widget layout ───────────────────────────────────────────────────────

    [Fact]
    public async Task Contributor_Can_Replace_Widget_Layout()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var client = CreateAuthenticatedClient(userId);
        var dashboards = await (await client.GetAsync($"/api/households/{householdId}/dashboards", TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<List<DashboardSummaryResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        var dashboardId = dashboards![0].Id;

        var layout = new ReplaceWidgetLayoutRequest([
            new WidgetDefinition(WidgetType.AssetCount, WidgetSize.Small, null),
            new WidgetDefinition(WidgetType.TotalHorsepower, WidgetSize.Wide, null),
            new WidgetDefinition(WidgetType.DueSoon, WidgetSize.Full, "{\"daysAhead\":14}"),
        ]);

        var response = await client.PutAsJsonAsync($"/api/households/{householdId}/dashboards/{dashboardId}/widgets", layout, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<DashboardDetailResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(3, detail!.Widgets.Count);
    }

    [Fact]
    public async Task Empty_Widget_Array_Clears_Dashboard()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var client = CreateAuthenticatedClient(userId);
        var dashboards = await (await client.GetAsync($"/api/households/{householdId}/dashboards", TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<List<DashboardSummaryResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        var dashboardId = dashboards![0].Id;

        var response = await client.PutAsJsonAsync($"/api/households/{householdId}/dashboards/{dashboardId}/widgets", new ReplaceWidgetLayoutRequest([]), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<DashboardDetailResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Empty(detail!.Widgets);
    }

    [Fact]
    public async Task Invalid_WidgetType_Returns_BadRequest()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var client = CreateAuthenticatedClient(userId);
        var dashboards = await (await client.GetAsync($"/api/households/{householdId}/dashboards", TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<List<DashboardSummaryResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        var dashboardId = dashboards![0].Id;

        var content = new StringContent(
            "{\"widgets\":[{\"widgetType\":\"WeatherForecast\",\"widgetSize\":\"Small\"}]}",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PutAsync($"/api/households/{householdId}/dashboards/{dashboardId}/widgets", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_Cannot_Replace_Widget_Layout()
    {
        var (householdId, ownerId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var ownerClient = CreateAuthenticatedClient(ownerId);
        var dashboards = await (await ownerClient.GetAsync($"/api/households/{householdId}/dashboards", TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<List<DashboardSummaryResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        var viewerId = Guid.NewGuid();
        await AddMemberAsync(householdId, viewerId, HouseholdMemberRole.Viewer);
        var viewerClient = CreateAuthenticatedClient(viewerId);

        var response = await viewerClient.PutAsJsonAsync($"/api/households/{householdId}/dashboards/{dashboards![0].Id}/widgets", new ReplaceWidgetLayoutRequest([]), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ─── Snapshot ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Snapshot_Empty_Dashboard_Returns_Empty_Object()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var client = CreateAuthenticatedClient(userId);
        var dashboards = await (await client.GetAsync($"/api/households/{householdId}/dashboards", TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<List<DashboardSummaryResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        var dashboardId = dashboards![0].Id;

        // Clear widgets
        await client.PutAsJsonAsync($"/api/households/{householdId}/dashboards/{dashboardId}/widgets", new ReplaceWidgetLayoutRequest([]), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        var response = await client.GetAsync($"/api/households/{householdId}/dashboards/{dashboardId}/snapshot", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
        Assert.Empty(doc.RootElement.EnumerateObject());
    }

    [Fact]
    public async Task Snapshot_AssetCount_Returns_Correct_Count()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);
        await CreateAssetAsync(householdId);
        await CreateAssetAsync(householdId);

        var dashboards = await (await client.GetAsync($"/api/households/{householdId}/dashboards", TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<List<DashboardSummaryResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        var dashboardId = dashboards![0].Id;

        await client.PutAsJsonAsync($"/api/households/{householdId}/dashboards/{dashboardId}/widgets", new ReplaceWidgetLayoutRequest([new WidgetDefinition(WidgetType.AssetCount, WidgetSize.Small, null)]), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        var response = await client.GetAsync($"/api/households/{householdId}/dashboards/{dashboardId}/snapshot", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var snapshot = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(snapshot);
        Assert.True(snapshot.ContainsKey("AssetCount"));
        Assert.Equal(2, snapshot["AssetCount"].GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task Snapshot_CylinderIndex_Excludes_Broken_And_Retired_Engines()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var client = CreateAuthenticatedClient(userId);
        var assetId = await CreateAssetAsync(householdId);

        // Active ICE engine with 4 cylinders
        await CreateEngineWithSpecsAsync(assetId, 4, EngineStatus.Active, EngineType.Ice);
        // Broken ICE engine with 4 cylinders (should be excluded)
        await CreateEngineWithSpecsAsync(assetId, 4, EngineStatus.Broken, EngineType.Ice);
        // Retired ICE engine (should be excluded)
        await CreateEngineWithSpecsAsync(assetId, 8, EngineStatus.Retired, EngineType.Ice);

        var dashboards = await (await client.GetAsync($"/api/households/{householdId}/dashboards", TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<List<DashboardSummaryResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        var dashboardId = dashboards![0].Id;

        await client.PutAsJsonAsync($"/api/households/{householdId}/dashboards/{dashboardId}/widgets", new ReplaceWidgetLayoutRequest([new WidgetDefinition(WidgetType.CylinderIndex, WidgetSize.Small, null)]), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        var response = await client.GetAsync($"/api/households/{householdId}/dashboards/{dashboardId}/snapshot", TestContext.Current.CancellationToken);

        var snapshot = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(4, snapshot!["CylinderIndex"].GetProperty("totalCylinders").GetInt32());
    }

    [Fact]
    public async Task Snapshot_Returns_Only_Keys_For_Widgets_In_Layout()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);

        var dashboards = await (await client.GetAsync($"/api/households/{householdId}/dashboards", TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<List<DashboardSummaryResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        var dashboardId = dashboards![0].Id;

        await client.PutAsJsonAsync($"/api/households/{householdId}/dashboards/{dashboardId}/widgets", new ReplaceWidgetLayoutRequest([
                new WidgetDefinition(WidgetType.AssetCount, WidgetSize.Small, null),
                new WidgetDefinition(WidgetType.DueSoon, WidgetSize.Full, null),
            ]), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        var response = await client.GetAsync($"/api/households/{householdId}/dashboards/{dashboardId}/snapshot", TestContext.Current.CancellationToken);

        var snapshot = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(snapshot);
        Assert.True(snapshot.ContainsKey("AssetCount"));
        Assert.True(snapshot.ContainsKey("DueSoon"));
        Assert.False(snapshot.ContainsKey("CylinderIndex"));
        Assert.False(snapshot.ContainsKey("TotalHorsepower"));
    }

    [Fact]
    public async Task Snapshot_DueSoon_Urgency_Classification()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);
        var assetId = await CreateAssetAsync(householdId);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await CreateRegistrationAsync(assetId, today.AddDays(-1));  // Overdue
        await CreateRegistrationAsync(assetId, today.AddDays(3));   // DueSoon (within 7 days)
        await CreateRegistrationAsync(assetId, today.AddDays(20));  // Upcoming (within 30 days)

        var dashboards = await (await client.GetAsync($"/api/households/{householdId}/dashboards", TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<List<DashboardSummaryResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        var dashboardId = dashboards![0].Id;

        await client.PutAsJsonAsync($"/api/households/{householdId}/dashboards/{dashboardId}/widgets", new ReplaceWidgetLayoutRequest([new WidgetDefinition(WidgetType.DueSoon, WidgetSize.Full, "{\"daysAhead\":30}")]), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        var response = await client.GetAsync($"/api/households/{householdId}/dashboards/{dashboardId}/snapshot", TestContext.Current.CancellationToken);

        var snapshot = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        var items = snapshot!["DueSoon"].GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(3, items.Count);
        Assert.Contains(items, i => i.GetProperty("urgency").GetString() == "Overdue");
        Assert.Contains(items, i => i.GetProperty("urgency").GetString() == "DueSoon");
        Assert.Contains(items, i => i.GetProperty("urgency").GetString() == "Upcoming");
    }

    [Fact]
    public async Task Snapshot_RecentActivity_Respects_Limit()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);
        var assetId = await CreateAssetAsync(householdId);

        for (var i = 0; i < 5; i++)
            await CreateServiceRecordAsync(assetId, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-i));

        var dashboards = await (await client.GetAsync($"/api/households/{householdId}/dashboards", TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<List<DashboardSummaryResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        var dashboardId = dashboards![0].Id;

        await client.PutAsJsonAsync($"/api/households/{householdId}/dashboards/{dashboardId}/widgets", new ReplaceWidgetLayoutRequest([new WidgetDefinition(WidgetType.RecentActivity, WidgetSize.Full, "{\"limit\":3}")]), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        var response = await client.GetAsync($"/api/households/{householdId}/dashboards/{dashboardId}/snapshot", TestContext.Current.CancellationToken);

        var snapshot = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        var items = snapshot!["RecentActivity"].GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(3, items.Count);
    }

    [Fact]
    public async Task Snapshot_FuelCostYtd_Scoped_To_Current_Year()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);
        var assetId = await CreateAssetAsync(householdId);

        var thisYear = new DateOnly(DateTime.UtcNow.Year, 6, 1);
        var lastYear = new DateOnly(DateTime.UtcNow.Year - 1, 6, 1);
        await CreateFuelLogAsync(assetId, thisYear, 100m);
        await CreateFuelLogAsync(assetId, lastYear, 200m);

        var dashboards = await (await client.GetAsync($"/api/households/{householdId}/dashboards", TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<List<DashboardSummaryResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        var dashboardId = dashboards![0].Id;

        await client.PutAsJsonAsync($"/api/households/{householdId}/dashboards/{dashboardId}/widgets", new ReplaceWidgetLayoutRequest([new WidgetDefinition(WidgetType.FuelCostYtd, WidgetSize.Small, null)]), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        var response = await client.GetAsync($"/api/households/{householdId}/dashboards/{dashboardId}/snapshot", TestContext.Current.CancellationToken);

        var snapshot = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(100m, snapshot!["FuelCostYtd"].GetProperty("totalCost").GetDecimal());
    }

    [Fact]
    public async Task Snapshot_MileageMtd_Scoped_To_Current_Month()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);
        var assetId = await CreateAssetAsync(householdId);

        var now = DateTime.UtcNow;
        var thisMonth = new DateOnly(now.Year, now.Month, 1);
        var lastMonth = new DateOnly(now.Year, now.Month, 1).AddMonths(-1);
        await CreateMileageLogAsync(assetId, thisMonth, 150m);
        await CreateMileageLogAsync(assetId, lastMonth, 300m);

        var dashboards = await (await client.GetAsync($"/api/households/{householdId}/dashboards", TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<List<DashboardSummaryResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        var dashboardId = dashboards![0].Id;

        await client.PutAsJsonAsync($"/api/households/{householdId}/dashboards/{dashboardId}/widgets", new ReplaceWidgetLayoutRequest([new WidgetDefinition(WidgetType.MileageMtd, WidgetSize.Small, null)]), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        var response = await client.GetAsync($"/api/households/{householdId}/dashboards/{dashboardId}/snapshot", TestContext.Current.CancellationToken);

        var snapshot = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(150m, snapshot!["MileageMtd"].GetProperty("totalMiles").GetDecimal());
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task CreateUserAsync(Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StewardDbContext>();
        db.Users.Add(new Steward.Infrastructure.Identity.ApplicationUser
        {
            Id = userId,
            UserName = $"test-{userId:N}@example.com",
            NormalizedUserName = $"TEST-{userId:N}@EXAMPLE.COM",
            Email = $"test-{userId:N}@example.com",
        });
        await db.SaveChangesAsync();
    }

    private async Task CreateEngineWithSpecsAsync(
        Guid assetId, int cylinders, EngineStatus status, EngineType engineType)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StewardDbContext>();
        db.Engines.Add(new Engine
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            Label = $"Engine-{Guid.NewGuid():N}",
            EngineType = engineType,
            FuelType = FuelType.Gasoline,
            Status = status,
            Cylinders = cylinders,
        });
        await db.SaveChangesAsync();
    }

    private async Task CreateRegistrationAsync(Guid assetId, DateOnly expiresOn)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StewardDbContext>();
        db.Registrations.Add(new Registration
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            RegistrationNumber = $"REG-{Guid.NewGuid():N}",
            ExpiresOn = expiresOn,
        });
        await db.SaveChangesAsync();
    }

    private async Task CreateServiceRecordAsync(Guid assetId, DateOnly date)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StewardDbContext>();
        db.ServiceRecords.Add(new ServiceRecord
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            Description = "Test service",
            Date = date,
        });
        await db.SaveChangesAsync();
    }

    private async Task CreateFuelLogAsync(Guid assetId, DateOnly date, decimal totalCost)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StewardDbContext>();
        db.FuelLogs.Add(new FuelLog
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            Date = date,
            Volume = 10m,
            VolumeUnit = VolumeUnit.Gallons,
            TotalCost = totalCost,
        });
        await db.SaveChangesAsync();
    }

    private async Task CreateMileageLogAsync(Guid assetId, DateOnly date, decimal tripMiles)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StewardDbContext>();
        db.MileageLogs.Add(new MileageLog
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            Date = date,
            TripMiles = tripMiles,
        });
        await db.SaveChangesAsync();
    }
}
