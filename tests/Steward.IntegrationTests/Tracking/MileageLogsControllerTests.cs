using System.Net;
using System.Net.Http.Json;
using Steward.Application.Tracking.MileageLogs;
using Steward.Domain.Enums;
using Steward.IntegrationTests.Infrastructure;

namespace Steward.IntegrationTests.Tracking;

public class MileageLogsControllerTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Contributor_Can_Create_List_Update_And_Delete_Mileage_Log()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var createResponse = await client.PostAsJsonAsync($"/api/households/{householdId}/assets/{assetId}/mileage-logs", NewLog(new DateOnly(2026, 6, 1)), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<MileageLogResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(created);

        var listResponse = await client.GetAsync($"/api/households/{householdId}/assets/{assetId}/mileage-logs", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<List<MileageLogResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(list!);

        var updateResponse = await client.PutAsJsonAsync($"/api/households/{householdId}/assets/{assetId}/mileage-logs/{created!.Id}", NewLog(new DateOnly(2026, 6, 1)) with { OdometerReading = 13000m }, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<MileageLogResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(13000m, updated!.OdometerReading);

        var deleteResponse = await client.DeleteAsync($"/api/households/{householdId}/assets/{assetId}/mileage-logs/{created.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Viewer_Cannot_Create_Mileage_Log()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Viewer);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/assets/{assetId}/mileage-logs", NewLog(new DateOnly(2026, 6, 1)), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Missing_OdometerReading_And_TripMiles_Returns_BadRequest()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/assets/{assetId}/mileage-logs", NewLog(new DateOnly(2026, 6, 1)) with { OdometerReading = null, TripMiles = null }, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Mileage_Log_Under_Different_Household_Asset_Returns_NotFound()
    {
        var (householdA, _) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdA);

        var (householdB, userB) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userB);

        var response = await client.GetAsync($"/api/households/{householdB}/assets/{assetId}/mileage-logs", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Date_Range_Filter_Excludes_Logs_Outside_Range()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        await client.PostAsJsonAsync($"/api/households/{householdId}/assets/{assetId}/mileage-logs", NewLog(new DateOnly(2026, 1, 15)), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        await client.PostAsJsonAsync($"/api/households/{householdId}/assets/{assetId}/mileage-logs", NewLog(new DateOnly(2026, 6, 15)), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        var response = await client.GetAsync($"/api/households/{householdId}/assets/{assetId}/mileage-logs?from=2026-01-01&to=2026-03-31", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var logs = await response.Content.ReadFromJsonAsync<List<MileageLogResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(logs!);
        Assert.Equal(new DateOnly(2026, 1, 15), logs![0].Date);
    }

    private static CreateMileageLogRequest NewLog(DateOnly date) => new(
        Date: date,
        OdometerReading: 12450m,
        TripMiles: null,
        Notes: null);
}
