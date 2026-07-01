using System.Net;
using System.Net.Http.Json;
using Steward.Application.Assets.Engines;
using Steward.Domain.Enums;
using Steward.IntegrationTests.Infrastructure;

namespace Steward.IntegrationTests.Engines;

public class EnginesControllerTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Contributor_Can_Add_Engine_To_Asset()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines", NewEngine("Port"), TestJson.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var engine = await response.Content.ReadFromJsonAsync<EngineResponse>(TestJson.Options);
        Assert.NotNull(engine);
        Assert.Equal("Port", engine.Label);
        Assert.Equal(EngineStatus.Active, engine.Status);
    }

    [Fact]
    public async Task Viewer_Cannot_Add_Engine()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Viewer);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines", NewEngine("Port"), TestJson.Options);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_Engine_Missing_Label_Returns_BadRequest()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines", NewEngine(""), TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Engine_For_Asset_In_Different_Household_Returns_NotFound()
    {
        var (householdA, _) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdA);

        var (householdB, userB) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userB);

        var response = await client.PostAsJsonAsync(
            $"/api/households/{householdB}/assets/{assetId}/engines", NewEngine("Port"), TestJson.Options);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Member_Can_List_Engines_Including_Retired()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var active = await CreateEngineAsync(client, householdId, assetId, NewEngine("Port"));
        var retired = await CreateEngineAsync(client, householdId, assetId, NewEngine("Starboard"));
        await client.PostAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines/{retired.Id}/retire", null);

        var response = await client.GetAsync($"/api/households/{householdId}/assets/{assetId}/engines");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var engines = await response.Content.ReadFromJsonAsync<List<EngineResponse>>(TestJson.Options);
        Assert.Equal(2, engines!.Count);
        Assert.Contains(engines, e => e.Id == active.Id && e.Status == EngineStatus.Active);
        Assert.Contains(engines, e => e.Id == retired.Id && e.Status == EngineStatus.Retired);
    }

    [Fact]
    public async Task Contributor_Can_Update_Engine_SerialNumber()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);
        var engine = await CreateEngineAsync(client, householdId, assetId, NewEngine("Port"));

        var updateRequest = NewEngineUpdate("Port") with { SerialNumber = "SN-1234" };
        var response = await client.PutAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines/{engine.Id}", updateRequest, TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EngineResponse>(TestJson.Options);
        Assert.Equal("SN-1234", updated!.SerialNumber);
    }

    [Fact]
    public async Task Viewer_Cannot_Update_Engine()
    {
        var (householdId, ownerId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var ownerClient = CreateAuthenticatedClient(ownerId);
        var engine = await CreateEngineAsync(ownerClient, householdId, assetId, NewEngine("Port"));

        var viewerId = Guid.NewGuid();
        await AddMemberAsync(householdId, viewerId, HouseholdMemberRole.Viewer);
        var viewerClient = CreateAuthenticatedClient(viewerId);

        var response = await viewerClient.PutAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines/{engine.Id}",
            NewEngineUpdate("Port") with { SerialNumber = "SN-1234" },
            TestJson.Options);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Contributor_Can_Retire_Active_Engine()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);
        var engine = await CreateEngineAsync(client, householdId, assetId, NewEngine("Port"));

        var response = await client.PostAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines/{engine.Id}/retire", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EngineResponse>(TestJson.Options);
        Assert.Equal(EngineStatus.Retired, updated!.Status);
    }

    [Fact]
    public async Task Retiring_Already_Retired_Engine_Returns_BadRequest()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);
        var engine = await CreateEngineAsync(client, householdId, assetId, NewEngine("Port"));
        await client.PostAsync($"/api/households/{householdId}/assets/{assetId}/engines/{engine.Id}/retire", null);

        var response = await client.PostAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines/{engine.Id}/retire", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Owner_Can_Reactivate_Retired_Engine()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);
        var engine = await CreateEngineAsync(client, householdId, assetId, NewEngine("Port"));
        await client.PostAsync($"/api/households/{householdId}/assets/{assetId}/engines/{engine.Id}/retire", null);

        var response = await client.PostAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines/{engine.Id}/reactivate", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EngineResponse>(TestJson.Options);
        Assert.Equal(EngineStatus.Active, updated!.Status);
    }

    [Fact]
    public async Task Reactivating_Active_Engine_Returns_BadRequest()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);
        var engine = await CreateEngineAsync(client, householdId, assetId, NewEngine("Port"));

        var response = await client.PostAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines/{engine.Id}/reactivate", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Owner_Can_Delete_Engine()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);
        var engine = await CreateEngineAsync(client, householdId, assetId, NewEngine("Port"));

        var response = await client.DeleteAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines/{engine.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Contributor_Cannot_Delete_Engine()
    {
        var (householdId, ownerId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var ownerClient = CreateAuthenticatedClient(ownerId);
        var engine = await CreateEngineAsync(ownerClient, householdId, assetId, NewEngine("Port"));

        var contributorId = Guid.NewGuid();
        await AddMemberAsync(householdId, contributorId, HouseholdMemberRole.Contributor);
        var contributorClient = CreateAuthenticatedClient(contributorId);

        var response = await contributorClient.DeleteAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines/{engine.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Contributor_Can_Create_Engine_With_Spec_Fields()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var request = NewEngine("Main") with
        {
            HorsepowerHp = 355m,
            TorqueNm = 475m,
            OilCapacityL = 5.7m,
            RecommendedOilType = "5W-30 Full Synthetic",
            RecommendedOctane = 91,
        };

        var response = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines", request, TestJson.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var engine = await response.Content.ReadFromJsonAsync<EngineResponse>(TestJson.Options);
        Assert.NotNull(engine);
        Assert.Equal(355m, engine.HorsepowerHp);
        Assert.Equal(475m, engine.TorqueNm);
        Assert.Equal(5.7m, engine.OilCapacityL);
        Assert.Equal("5W-30 Full Synthetic", engine.RecommendedOilType);
        Assert.Equal(91, engine.RecommendedOctane);
    }

    [Fact]
    public async Task Create_Engine_Without_Spec_Fields_Returns_Nulls()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines", NewEngine("Port"), TestJson.Options);

        var engine = await response.Content.ReadFromJsonAsync<EngineResponse>(TestJson.Options);
        Assert.Null(engine!.HorsepowerHp);
        Assert.Null(engine.TorqueNm);
        Assert.Null(engine.OilCapacityL);
        Assert.Null(engine.RecommendedOilType);
        Assert.Null(engine.CoolantCapacityL);
        Assert.Null(engine.RecommendedOctane);
    }

    [Fact]
    public async Task MarkBroken_Transitions_Active_To_Broken()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);
        var engine = await CreateEngineAsync(client, householdId, assetId, NewEngine("Port"));

        var response = await client.PostAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines/{engine.Id}/mark-broken", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EngineResponse>(TestJson.Options);
        Assert.Equal(EngineStatus.Broken, updated!.Status);
    }

    [Fact]
    public async Task Reactivate_From_Broken_Transitions_To_Active()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);
        var engine = await CreateEngineAsync(client, householdId, assetId, NewEngine("Port"));
        await client.PostAsync($"/api/households/{householdId}/assets/{assetId}/engines/{engine.Id}/mark-broken", null);

        var response = await client.PostAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines/{engine.Id}/reactivate", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EngineResponse>(TestJson.Options);
        Assert.Equal(EngineStatus.Active, updated!.Status);
    }

    [Fact]
    public async Task Retire_From_Broken_Transitions_To_Retired()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);
        var engine = await CreateEngineAsync(client, householdId, assetId, NewEngine("Port"));
        await client.PostAsync($"/api/households/{householdId}/assets/{assetId}/engines/{engine.Id}/mark-broken", null);

        var response = await client.PostAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines/{engine.Id}/retire", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EngineResponse>(TestJson.Options);
        Assert.Equal(EngineStatus.Retired, updated!.Status);
    }

    [Fact]
    public async Task MarkBroken_Already_Broken_Returns_BadRequest()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);
        var engine = await CreateEngineAsync(client, householdId, assetId, NewEngine("Port"));
        await client.PostAsync($"/api/households/{householdId}/assets/{assetId}/engines/{engine.Id}/mark-broken", null);

        var response = await client.PostAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines/{engine.Id}/mark-broken", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MarkBroken_Retired_Engine_Returns_BadRequest()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);
        var engine = await CreateEngineAsync(client, householdId, assetId, NewEngine("Port"));
        await client.PostAsync($"/api/households/{householdId}/assets/{assetId}/engines/{engine.Id}/retire", null);

        var response = await client.PostAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines/{engine.Id}/mark-broken", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RecommendedOctane_Invalid_Returns_BadRequest()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var request = NewEngine("Port") with { RecommendedOctane = 94 };
        var response = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines", request, TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static CreateEngineRequest NewEngine(string label) => new(
        Label: label,
        Make: null,
        Model: null,
        SerialNumber: null,
        Year: null,
        EngineType: EngineType.Ice,
        FuelType: FuelType.Gasoline,
        Cylinders: null,
        DisplacementCc: null,
        InstalledDate: null,
        InstalledAtAssetMiles: null,
        InstalledAtAssetHours: null,
        HorsepowerHp: null,
        TorqueNm: null,
        OilCapacityL: null,
        RecommendedOilType: null,
        CoolantCapacityL: null,
        RecommendedOctane: null);

    private static UpdateEngineRequest NewEngineUpdate(string label) => new(
        Label: label,
        Make: null,
        Model: null,
        SerialNumber: null,
        Year: null,
        EngineType: EngineType.Ice,
        FuelType: FuelType.Gasoline,
        Cylinders: null,
        DisplacementCc: null,
        InstalledDate: null,
        InstalledAtAssetMiles: null,
        InstalledAtAssetHours: null,
        HorsepowerHp: null,
        TorqueNm: null,
        OilCapacityL: null,
        RecommendedOilType: null,
        CoolantCapacityL: null,
        RecommendedOctane: null);

    private static async Task<EngineResponse> CreateEngineAsync(
        HttpClient client, Guid householdId, Guid assetId, CreateEngineRequest request)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/engines", request, TestJson.Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EngineResponse>(TestJson.Options))!;
    }
}
