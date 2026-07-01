using System.Net;
using System.Net.Http.Json;
using Steward.Application.Assets;
using Steward.Domain.Enums;
using Steward.IntegrationTests.Infrastructure;

namespace Steward.IntegrationTests.Assets;

public class AssetsControllerTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Contributor_Can_Create_Vehicle_Asset()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var client = CreateAuthenticatedClient(userId);
        var request = NewAsset(AssetType.Boat, "Sea Ray") with
        {
            Hin = "ABC12345D404",
            LengthFt = 24.5m,
            BeamFt = 8.5m,
        };

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/assets", request, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var asset = await response.Content.ReadFromJsonAsync<AssetResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(asset);
        Assert.Equal(AssetType.Boat, asset.AssetType);
        Assert.Equal("ABC12345D404", asset.Hin);
    }

    [Fact]
    public async Task Contributor_Can_Create_Trailer_Asset()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var client = CreateAuthenticatedClient(userId);
        var request = NewAsset(AssetType.EnclosedTrailer, "Cargo Trailer") with
        {
            InteriorHeightFt = 7.0m,
            InteriorLengthFt = 16.0m,
        };

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/assets", request, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var asset = await response.Content.ReadFromJsonAsync<AssetResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(asset);
        Assert.Equal(AssetType.EnclosedTrailer, asset.AssetType);
    }

    [Fact]
    public async Task Contributor_Can_Create_Equipment_Asset()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var client = CreateAuthenticatedClient(userId);
        var request = NewAsset(AssetType.RidingMower, "Lawn Mower") with
        {
            Make = "Husqvarna",
            Model = "YTH24V48",
            CuttingWidthIn = 48.0m,
        };

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/assets", request, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var asset = await response.Content.ReadFromJsonAsync<AssetResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(asset);
        Assert.Equal(AssetType.RidingMower, asset.AssetType);
    }

    [Fact]
    public async Task Viewer_Cannot_Create_Asset()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Viewer);
        var client = CreateAuthenticatedClient(userId);
        var request = NewAsset(AssetType.Snowmobile, "Ski-Doo");

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/assets", request, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NonMember_Cannot_Create_Asset()
    {
        var householdId = await CreateHouseholdAsync();
        var client = CreateAuthenticatedClient(Guid.NewGuid());
        var request = NewAsset(AssetType.Snowmobile, "Ski-Doo");

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/assets", request, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_With_Unknown_AssetType_Returns_BadRequest()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/assets", new { assetType = "Spaceship", name = "Rocket" }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Member_Can_List_And_Filter_Assets_By_Type()
    {
        var (householdId, ownerId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(ownerId);

        await CreateAssetAsync(client, householdId, NewAsset(AssetType.Snowmobile, "Ski-Doo"));
        await CreateAssetAsync(client, householdId, NewAsset(AssetType.Boat, "Sea Ray") with { Hin = "ABC12345D404" });

        var listResponse = await client.GetAsync($"/api/households/{householdId}/assets", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var all = await listResponse.Content.ReadFromJsonAsync<List<AssetResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(2, all!.Count);

        var filteredResponse = await client.GetAsync($"/api/households/{householdId}/assets?assetType=Boat", TestContext.Current.CancellationToken);
        var filtered = await filteredResponse.Content.ReadFromJsonAsync<List<AssetResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(filtered!);
        Assert.Equal(AssetType.Boat, filtered![0].AssetType);
    }

    [Fact]
    public async Task NonMember_Cannot_List_Assets()
    {
        var householdId = await CreateHouseholdAsync();
        var client = CreateAuthenticatedClient(Guid.NewGuid());

        var response = await client.GetAsync($"/api/households/{householdId}/assets", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Contributor_Can_Update_Asset_Name()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var client = CreateAuthenticatedClient(userId);
        var created = await CreateAssetAsync(client, householdId, NewAsset(AssetType.Snowmobile, "Ski-Doo"));

        var updateRequest = NewUpdate("Ski-Doo Renamed");

        var response = await client.PutAsJsonAsync($"/api/households/{householdId}/assets/{created.Id}", updateRequest, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<AssetResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("Ski-Doo Renamed", updated!.Name);
    }

    [Fact]
    public async Task Update_With_Mismatched_AssetType_Returns_BadRequest()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);
        var created = await CreateAssetAsync(client, householdId, NewAsset(AssetType.Snowmobile, "Ski-Doo"));

        var updateRequest = NewUpdate("Ski-Doo") with { AssetType = AssetType.Boat };

        var response = await client.PutAsJsonAsync($"/api/households/{householdId}/assets/{created.Id}", updateRequest, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_Cannot_Update_Asset()
    {
        var (householdId, ownerId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var ownerClient = CreateAuthenticatedClient(ownerId);
        var created = await CreateAssetAsync(ownerClient, householdId, NewAsset(AssetType.Snowmobile, "Ski-Doo"));

        var viewerId = Guid.NewGuid();
        await AddMemberAsync(householdId, viewerId, HouseholdMemberRole.Viewer);
        var viewerClient = CreateAuthenticatedClient(viewerId);

        var response = await viewerClient.PutAsJsonAsync($"/api/households/{householdId}/assets/{created.Id}", NewUpdate("Renamed"), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Owner_Can_Delete_Asset()
    {
        var (householdId, ownerId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(ownerId);
        var created = await CreateAssetAsync(client, householdId, NewAsset(AssetType.Snowmobile, "Ski-Doo"));

        var response = await client.DeleteAsync($"/api/households/{householdId}/assets/{created.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var getResponse = await client.GetAsync($"/api/households/{householdId}/assets/{created.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Contributor_Cannot_Delete_Asset()
    {
        var (householdId, ownerId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var ownerClient = CreateAuthenticatedClient(ownerId);
        var created = await CreateAssetAsync(ownerClient, householdId, NewAsset(AssetType.Snowmobile, "Ski-Doo"));

        var contributorId = Guid.NewGuid();
        await AddMemberAsync(householdId, contributorId, HouseholdMemberRole.Contributor);
        var contributorClient = CreateAuthenticatedClient(contributorId);

        var response = await contributorClient.DeleteAsync($"/api/households/{householdId}/assets/{created.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static CreateAssetRequest NewAsset(AssetType assetType, string name) => new(
        AssetType: assetType,
        Name: name,
        Description: null,
        Year: null,
        PhotoUrl: null,
        UsageTrackingMode: UsageTrackingMode.None,
        Vin: null,
        Color: null,
        Make: null,
        Model: null,
        Hin: null,
        HullMaterial: null,
        LengthFt: null,
        BeamFt: null,
        TrackLengthIn: null,
        BallSizeIn: null,
        MaxLoadLbs: null,
        InteriorHeightFt: null,
        InteriorLengthFt: null,
        CuttingWidthIn: null,
        MaxPsi: null,
        MaxGpm: null,
        EquipmentDescription: null);

    private static UpdateAssetRequest NewUpdate(string name) => new(
        AssetType: null,
        Name: name,
        Description: null,
        Year: null,
        PhotoUrl: null,
        UsageTrackingMode: UsageTrackingMode.None,
        Vin: null,
        Color: null,
        Make: null,
        Model: null,
        Hin: null,
        HullMaterial: null,
        LengthFt: null,
        BeamFt: null,
        TrackLengthIn: null,
        BallSizeIn: null,
        MaxLoadLbs: null,
        InteriorHeightFt: null,
        InteriorLengthFt: null,
        CuttingWidthIn: null,
        MaxPsi: null,
        MaxGpm: null,
        EquipmentDescription: null);

    private static async Task<AssetResponse> CreateAssetAsync(
        HttpClient client, Guid householdId, CreateAssetRequest request)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets", request, TestJson.Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AssetResponse>(TestJson.Options))!;
    }
}
