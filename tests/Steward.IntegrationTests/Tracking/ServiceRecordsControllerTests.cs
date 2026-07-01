using System.Net;
using System.Net.Http.Json;
using Steward.Application.Tracking.ServiceRecords;
using Steward.Domain.Enums;
using Steward.IntegrationTests.Infrastructure;

namespace Steward.IntegrationTests.Tracking;

public class ServiceRecordsControllerTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Contributor_Can_Create_List_Update_And_Delete_Service_Record()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var createResponse = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/service-records",
            NewRecord(new DateOnly(2026, 6, 1)),
            TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ServiceRecordResponse>(TestJson.Options);
        Assert.NotNull(created);

        var listResponse = await client.GetAsync($"/api/households/{householdId}/assets/{assetId}/service-records");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<List<ServiceRecordResponse>>(TestJson.Options);
        Assert.Single(list!);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/service-records/{created!.Id}",
            NewRecord(new DateOnly(2026, 6, 1)) with { Cost = 99.99m },
            TestJson.Options);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ServiceRecordResponse>(TestJson.Options);
        Assert.Equal(99.99m, updated!.Cost);

        var deleteResponse = await client.DeleteAsync(
            $"/api/households/{householdId}/assets/{assetId}/service-records/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Viewer_Cannot_Create_Service_Record()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Viewer);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/service-records",
            NewRecord(new DateOnly(2026, 6, 1)),
            TestJson.Options);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Missing_Description_Returns_BadRequest()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/service-records",
            NewRecord(new DateOnly(2026, 6, 1)) with { Description = "" },
            TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EngineId_From_Different_Asset_Rejected()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var otherAssetId = await CreateAssetAsync(householdId, "Other Asset");
        var otherEngineId = await CreateEngineAsync(otherAssetId);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/service-records",
            NewRecord(new DateOnly(2026, 6, 1)) with { EngineId = otherEngineId },
            TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Service_Record_Under_Different_Household_Asset_Returns_NotFound()
    {
        var (householdA, userA) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdA);

        var (householdB, userB) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userB);

        var response = await client.GetAsync($"/api/households/{householdB}/assets/{assetId}/service-records");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Date_Range_Filter_Excludes_Records_Outside_Range()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/service-records",
            NewRecord(new DateOnly(2026, 1, 15)),
            TestJson.Options);
        await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/service-records",
            NewRecord(new DateOnly(2026, 6, 15)),
            TestJson.Options);

        var response = await client.GetAsync(
            $"/api/households/{householdId}/assets/{assetId}/service-records?from=2026-01-01&to=2026-03-31");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var records = await response.Content.ReadFromJsonAsync<List<ServiceRecordResponse>>(TestJson.Options);
        Assert.Single(records!);
        Assert.Equal(new DateOnly(2026, 1, 15), records![0].Date);
    }

    private static CreateServiceRecordRequest NewRecord(DateOnly date) => new(
        Date: date,
        Description: "Oil change",
        ProviderName: null,
        Cost: 85.00m,
        OdometerMiles: null,
        EngineHours: null,
        EngineId: null,
        Notes: null);
}
