using System.Net;
using System.Net.Http.Json;
using Steward.Application.Tracking.MaintenanceItems;
using Steward.Domain.Enums;
using Steward.IntegrationTests.Infrastructure;

namespace Steward.IntegrationTests.Maintenance;

public class HouseholdMaintenanceItemsControllerTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Lists_Items_Across_Every_Asset_In_The_Household()
    {
        var ct = TestContext.Current.CancellationToken;
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetA = await CreateAssetAsync(householdId, "Asset A");
        var assetB = await CreateAssetAsync(householdId, "Asset B");
        var assetC = await CreateAssetAsync(householdId, "Asset C");
        var client = CreateAuthenticatedClient(userId);

        foreach (var assetId in new[] { assetA, assetB, assetC })
        {
            await client.PostAsJsonAsync(
                $"/api/households/{householdId}/assets/{assetId}/maintenance-items",
                new CreateMaintenanceItemRequest("Oil change", null, null, null, null, null, null, null, null, null),
                TestJson.Options, ct);
        }

        var response = await client.GetAsync($"/api/households/{householdId}/maintenance-items", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<HouseholdMaintenanceItemResponse>>(TestJson.Options, ct);
        Assert.Equal(3, items!.Count);
        Assert.Contains(items, i => i.AssetName == "Asset A");
        Assert.Contains(items, i => i.AssetName == "Asset B");
        Assert.Contains(items, i => i.AssetName == "Asset C");
    }

    [Fact]
    public async Task Filters_By_Status_And_Asset_Together()
    {
        var ct = TestContext.Current.CancellationToken;
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetA = await CreateAssetAsync(householdId, "Asset A");
        var assetB = await CreateAssetAsync(householdId, "Asset B");
        var client = CreateAuthenticatedClient(userId);

        await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetA}/maintenance-items",
            new CreateMaintenanceItemRequest("Planned on A", null, null, MaintenanceItemStatus.Planned, null, null, null, null, null, null),
            TestJson.Options, ct);
        await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetA}/maintenance-items",
            new CreateMaintenanceItemRequest("Done on A", null, null, MaintenanceItemStatus.Done, new DateOnly(2026, 1, 1), null, null, null, null, null),
            TestJson.Options, ct);
        await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetB}/maintenance-items",
            new CreateMaintenanceItemRequest("Planned on B", null, null, MaintenanceItemStatus.Planned, null, null, null, null, null, null),
            TestJson.Options, ct);

        var response = await client.GetAsync(
            $"/api/households/{householdId}/maintenance-items?status=Planned&status=InProgress&assetId={assetA}", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<HouseholdMaintenanceItemResponse>>(TestJson.Options, ct);
        var item = Assert.Single(items!);
        Assert.Equal("Planned on A", item.Title);
    }

    [Fact]
    public async Task NonMember_Cannot_List()
    {
        var ct = TestContext.Current.CancellationToken;
        var (householdId, _) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var nonMemberClient = CreateAuthenticatedClient(Guid.NewGuid());

        var response = await nonMemberClient.GetAsync($"/api/households/{householdId}/maintenance-items", ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
