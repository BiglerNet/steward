using System.Net;
using System.Net.Http.Json;
using Steward.Application.Tracking.Warranties;
using Steward.Domain.Enums;
using Steward.IntegrationTests.Infrastructure;

namespace Steward.IntegrationTests.Tracking;

public class WarrantiesControllerTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Contributor_Can_Create_List_Update_And_Delete_Warranty()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var createResponse = await client.PostAsJsonAsync($"/api/households/{householdId}/assets/{assetId}/warranties", NewRecord(), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<WarrantyResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(created);
        Assert.False(created!.HasDocument);

        var listResponse = await client.GetAsync($"/api/households/{householdId}/assets/{assetId}/warranties", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<List<WarrantyResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(list!);

        var updateResponse = await client.PutAsJsonAsync($"/api/households/{householdId}/assets/{assetId}/warranties/{created.Id}", NewRecord() with { Description = "Updated description" }, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<WarrantyResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("Updated description", updated!.Description);

        var deleteResponse = await client.DeleteAsync($"/api/households/{householdId}/assets/{assetId}/warranties/{created.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Viewer_Cannot_Create_Warranty()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Viewer);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/assets/{assetId}/warranties", NewRecord(), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Contributor_Can_Delete_Warranty()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var createResponse = await client.PostAsJsonAsync($"/api/households/{householdId}/assets/{assetId}/warranties", NewRecord(), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<WarrantyResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        var deleteResponse = await client.DeleteAsync($"/api/households/{householdId}/assets/{assetId}/warranties/{created!.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Missing_Provider_Returns_BadRequest()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/assets/{assetId}/warranties", NewRecord() with { Provider = "" }, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Warranty_Under_Different_Household_Asset_Returns_NotFound()
    {
        var (householdA, _) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdA);

        var (householdB, userB) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userB);

        var response = await client.GetAsync($"/api/households/{householdB}/assets/{assetId}/warranties", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static CreateWarrantyRequest NewRecord() => new(
        Provider: "Mercury Marine",
        Description: "Engine warranty",
        StartsOn: new DateOnly(2026, 1, 1),
        ExpiresOn: new DateOnly(2028, 1, 1),
        Notes: null);
}
