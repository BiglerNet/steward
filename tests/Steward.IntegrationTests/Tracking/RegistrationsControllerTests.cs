using System.Net;
using System.Net.Http.Json;
using Steward.Application.Tracking.Registrations;
using Steward.Domain.Enums;
using Steward.IntegrationTests.Infrastructure;

namespace Steward.IntegrationTests.Tracking;

public class RegistrationsControllerTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Contributor_Can_Create_List_Update_And_Delete_Registration()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var createResponse = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/registrations",
            NewRecord(new DateOnly(2027, 1, 15)),
            TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<RegistrationResponse>(TestJson.Options);
        Assert.NotNull(created);
        Assert.False(created!.HasDocument);

        var listResponse = await client.GetAsync($"/api/households/{householdId}/assets/{assetId}/registrations");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<List<RegistrationResponse>>(TestJson.Options);
        Assert.Single(list!);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/registrations/{created.Id}",
            NewRecord(new DateOnly(2027, 1, 15)) with { Cost = 150.00m },
            TestJson.Options);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<RegistrationResponse>(TestJson.Options);
        Assert.Equal(150.00m, updated!.Cost);

        var deleteResponse = await client.DeleteAsync(
            $"/api/households/{householdId}/assets/{assetId}/registrations/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Viewer_Cannot_Create_Registration()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Viewer);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/registrations",
            NewRecord(new DateOnly(2027, 1, 15)),
            TestJson.Options);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Contributor_Can_Delete_Registration()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var createResponse = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/registrations",
            NewRecord(new DateOnly(2027, 1, 15)),
            TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<RegistrationResponse>(TestJson.Options);

        var deleteResponse = await client.DeleteAsync(
            $"/api/households/{householdId}/assets/{assetId}/registrations/{created!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Missing_RegistrationNumber_Returns_BadRequest()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/registrations",
            NewRecord(new DateOnly(2027, 1, 15)) with { RegistrationNumber = "" },
            TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Multiple_Renewals_Are_Preserved_As_History_Ordered_By_ExpiresOn_Descending()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/registrations",
            NewRecord(new DateOnly(2026, 1, 15)),
            TestJson.Options);
        await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/registrations",
            NewRecord(new DateOnly(2028, 1, 15)),
            TestJson.Options);
        var thirdResponse = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/registrations",
            NewRecord(new DateOnly(2027, 1, 15)),
            TestJson.Options);
        var third = await thirdResponse.Content.ReadFromJsonAsync<RegistrationResponse>(TestJson.Options);

        var listResponse = await client.GetAsync($"/api/households/{householdId}/assets/{assetId}/registrations");
        var list = await listResponse.Content.ReadFromJsonAsync<List<RegistrationResponse>>(TestJson.Options);

        Assert.Equal(3, list!.Count);
        Assert.Equal(new DateOnly(2028, 1, 15), list[0].ExpiresOn);
        Assert.Equal(new DateOnly(2027, 1, 15), list[1].ExpiresOn);
        Assert.Equal(new DateOnly(2026, 1, 15), list[2].ExpiresOn);

        var deleteResponse = await client.DeleteAsync(
            $"/api/households/{householdId}/assets/{assetId}/registrations/{third!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var afterDeleteResponse = await client.GetAsync($"/api/households/{householdId}/assets/{assetId}/registrations");
        var afterDelete = await afterDeleteResponse.Content.ReadFromJsonAsync<List<RegistrationResponse>>(TestJson.Options);
        Assert.Equal(2, afterDelete!.Count);
        Assert.DoesNotContain(afterDelete, r => r.Id == third.Id);
    }

    [Fact]
    public async Task Registration_Under_Different_Household_Asset_Returns_NotFound()
    {
        var (householdA, _) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdA);

        var (householdB, userB) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userB);

        var response = await client.GetAsync($"/api/households/{householdB}/assets/{assetId}/registrations");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static CreateRegistrationRequest NewRecord(DateOnly expiresOn) => new(
        RegistrationNumber: "ABC-1234",
        IssuingAuthority: "DMV",
        RenewedOn: expiresOn.AddYears(-1),
        Cost: 120.00m,
        ExpiresOn: expiresOn,
        Notes: null);
}
