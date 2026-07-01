using System.Net;
using System.Net.Http.Json;
using Steward.Application.Tracking.FuelLogs;
using Steward.Domain.Enums;
using Steward.IntegrationTests.Infrastructure;

namespace Steward.IntegrationTests.Tracking;

public class FuelLogsControllerTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Contributor_Can_Create_List_Update_And_Delete_Fuel_Log()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var createResponse = await client.PostAsJsonAsync($"/api/households/{householdId}/assets/{assetId}/fuel-logs", NewLog(new DateOnly(2026, 6, 1)), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<FuelLogResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(created);

        var listResponse = await client.GetAsync($"/api/households/{householdId}/assets/{assetId}/fuel-logs", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<List<FuelLogResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(list!);

        var updateResponse = await client.PutAsJsonAsync($"/api/households/{householdId}/assets/{assetId}/fuel-logs/{created!.Id}", NewLog(new DateOnly(2026, 6, 1)) with { TotalCost = 60.00m }, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<FuelLogResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(60.00m, updated!.TotalCost);

        var deleteResponse = await client.DeleteAsync($"/api/households/{householdId}/assets/{assetId}/fuel-logs/{created.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Viewer_Cannot_Create_Fuel_Log()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Viewer);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/assets/{assetId}/fuel-logs", NewLog(new DateOnly(2026, 6, 1)), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_LogType_Returns_BadRequest()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var payload = new
        {
            logType = "Refund",
            date = "2026-06-01",
            volume = 12.5m,
            volumeUnit = "Gallons",
        };

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/assets/{assetId}/fuel-logs", payload, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

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

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/assets/{assetId}/fuel-logs", NewLog(new DateOnly(2026, 6, 1)) with { EngineId = otherEngineId }, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Fuel_Log_Under_Different_Household_Asset_Returns_NotFound()
    {
        var (householdA, _) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdA);

        var (householdB, userB) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userB);

        var response = await client.GetAsync($"/api/households/{householdB}/assets/{assetId}/fuel-logs", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Date_Range_Filter_Excludes_Logs_Outside_Range()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        await client.PostAsJsonAsync($"/api/households/{householdId}/assets/{assetId}/fuel-logs", NewLog(new DateOnly(2026, 1, 15)), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        await client.PostAsJsonAsync($"/api/households/{householdId}/assets/{assetId}/fuel-logs", NewLog(new DateOnly(2026, 6, 15)), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        var response = await client.GetAsync($"/api/households/{householdId}/assets/{assetId}/fuel-logs?from=2026-01-01&to=2026-03-31", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var logs = await response.Content.ReadFromJsonAsync<List<FuelLogResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(logs!);
        Assert.Equal(new DateOnly(2026, 1, 15), logs![0].Date);
    }

    private static CreateFuelLogRequest NewLog(DateOnly date) => new(
        LogType: FuelLogType.Fillup,
        Date: date,
        Volume: 12.5m,
        VolumeUnit: VolumeUnit.Gallons,
        FuelGrade: null,
        PricePerUnit: null,
        TotalCost: 48.75m,
        MilesAtLog: null,
        HoursAtLog: null,
        EngineId: null,
        Notes: null);
}
