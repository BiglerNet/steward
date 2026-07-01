using System.Net;
using System.Net.Http.Json;
using Steward.Application.Tracking.EngineHoursLogs;
using Steward.Domain.Enums;
using Steward.IntegrationTests.Infrastructure;

namespace Steward.IntegrationTests.Tracking;

public class EngineHoursLogsControllerTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Contributor_Can_Create_List_Update_And_Delete_Hours_Log()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var engineId = await CreateEngineAsync(assetId);
        var client = CreateAuthenticatedClient(userId);

        var createResponse = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines/{engineId}/hours-logs",
            NewLog(new DateOnly(2026, 6, 1)),
            TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<EngineHoursLogResponse>(TestJson.Options);
        Assert.NotNull(created);

        var listResponse = await client.GetAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines/{engineId}/hours-logs");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<List<EngineHoursLogResponse>>(TestJson.Options);
        Assert.Single(list!);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines/{engineId}/hours-logs/{created!.Id}",
            NewLog(new DateOnly(2026, 6, 1)) with { HoursReading = 350m },
            TestJson.Options);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<EngineHoursLogResponse>(TestJson.Options);
        Assert.Equal(350m, updated!.HoursReading);

        var deleteResponse = await client.DeleteAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines/{engineId}/hours-logs/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Viewer_Cannot_Create_Hours_Log()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Viewer);
        var assetId = await CreateAssetAsync(householdId);
        var engineId = await CreateEngineAsync(assetId);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines/{engineId}/hours-logs",
            NewLog(new DateOnly(2026, 6, 1)),
            TestJson.Options);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Missing_HoursReading_And_TripHours_Returns_BadRequest()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var engineId = await CreateEngineAsync(assetId);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines/{engineId}/hours-logs",
            NewLog(new DateOnly(2026, 6, 1)) with { HoursReading = null, TripHours = null },
            TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Engine_From_Different_Asset_Returns_NotFound()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var otherAssetId = await CreateAssetAsync(householdId, "Other Asset");
        var engineOnOtherAsset = await CreateEngineAsync(otherAssetId);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines/{engineOnOtherAsset}/hours-logs",
            NewLog(new DateOnly(2026, 6, 1)),
            TestJson.Options);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Hours_Log_Under_Different_Household_Returns_NotFound()
    {
        var (householdA, _) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdA);
        var engineId = await CreateEngineAsync(assetId);

        var (householdB, userB) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userB);

        var response = await client.GetAsync(
            $"/api/households/{householdB}/assets/{assetId}/engines/{engineId}/hours-logs");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Date_Range_Filter_Excludes_Logs_Outside_Range()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var engineId = await CreateEngineAsync(assetId);
        var client = CreateAuthenticatedClient(userId);

        await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines/{engineId}/hours-logs",
            NewLog(new DateOnly(2026, 1, 15)),
            TestJson.Options);
        await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines/{engineId}/hours-logs",
            NewLog(new DateOnly(2026, 6, 15)),
            TestJson.Options);

        var response = await client.GetAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines/{engineId}/hours-logs?from=2026-01-01&to=2026-03-31");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var logs = await response.Content.ReadFromJsonAsync<List<EngineHoursLogResponse>>(TestJson.Options);
        Assert.Single(logs!);
        Assert.Equal(new DateOnly(2026, 1, 15), logs![0].Date);
    }

    private static CreateEngineHoursLogRequest NewLog(DateOnly date) => new(
        Date: date,
        HoursReading: 340.5m,
        TripHours: null,
        Notes: null);
}
