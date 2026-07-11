using System.Net;
using System.Net.Http.Json;
using Steward.Application.Assets;
using Steward.Application.AssetTypes;
using Steward.Domain.Enums;
using Steward.IntegrationTests.Infrastructure;
using DriveType = Steward.Domain.Enums.DriveType;

namespace Steward.IntegrationTests.Assets;

public class AssetsControllerTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Contributor_Can_Create_PowerBoat_Asset()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var client = CreateAuthenticatedClient(userId);
        var request = NewAsset(AssetCategory.PowerBoat, "Sea Ray") with
        {
            Hin = "ABC12345D404",
            HullType = HullType.Monohull,
            DriveType = DriveType.SternDrive,
            LengthFt = 24.5m,
            BeamFt = 8.5m,
        };

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/assets", request, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var asset = await response.Content.ReadFromJsonAsync<AssetResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(asset);
        Assert.Equal(AssetCategory.PowerBoat, asset.Category);
        Assert.Equal(AssetStructuralType.Boat, asset.StructuralType);
        Assert.Equal("ABC12345D404", asset.Hin);
        Assert.Equal(DriveType.SternDrive, asset.DriveType);
    }

    [Fact]
    public async Task Contributor_Can_Create_Sailboat_Asset_With_Rig_Fields()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var client = CreateAuthenticatedClient(userId);
        var request = NewAsset(AssetCategory.Sailboat, "Wind Dancer") with
        {
            Hin = "XYZ98765E505",
            HullType = HullType.Monohull,
            KeelType = "Fin",
            MastHeightFt = 42m,
            MastCount = 1,
        };

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/assets", request, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var asset = await response.Content.ReadFromJsonAsync<AssetResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(asset);
        Assert.Equal(AssetCategory.Sailboat, asset.Category);
        Assert.Equal(AssetStructuralType.Boat, asset.StructuralType);
        Assert.Equal("Fin", asset.KeelType);
        Assert.Equal(42m, asset.MastHeightFt);
        Assert.Equal(1, asset.MastCount);
    }

    [Fact]
    public async Task Create_Sailboat_With_DriveType_Returns_BadRequest()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);
        var request = NewAsset(AssetCategory.Sailboat, "Wind Dancer") with { DriveType = DriveType.Outboard };

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/assets", request, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("driveType", body);
    }

    [Fact]
    public async Task Category_Maps_To_Structural_Type()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var client = CreateAuthenticatedClient(userId);
        var request = NewAsset(AssetCategory.Snowmobile, "Ski-Doo") with { TrackLengthIn = 137m };

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/assets", request, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var asset = await response.Content.ReadFromJsonAsync<AssetResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(asset);
        Assert.Equal(AssetCategory.Snowmobile, asset.Category);
        Assert.Equal(AssetStructuralType.Vehicle, asset.StructuralType);
        Assert.Equal(137m, asset.TrackLengthIn);
    }

    [Fact]
    public async Task Contributor_Can_Create_Trailer_Asset()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var client = CreateAuthenticatedClient(userId);
        var request = NewAsset(AssetCategory.EnclosedTrailer, "Cargo Trailer") with
        {
            InteriorHeightFt = 7.0m,
            InteriorLengthFt = 16.0m,
        };

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/assets", request, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var asset = await response.Content.ReadFromJsonAsync<AssetResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(asset);
        Assert.Equal(AssetCategory.EnclosedTrailer, asset.Category);
        Assert.Equal(AssetStructuralType.Trailer, asset.StructuralType);
    }

    [Fact]
    public async Task Contributor_Can_Create_Equipment_Asset()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var client = CreateAuthenticatedClient(userId);
        var request = NewAsset(AssetCategory.RidingMower, "Lawn Mower") with
        {
            CuttingWidthIn = 48.0m,
        };

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/assets", request, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var asset = await response.Content.ReadFromJsonAsync<AssetResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(asset);
        Assert.Equal(AssetCategory.RidingMower, asset.Category);
        Assert.Equal(AssetStructuralType.Equipment, asset.StructuralType);
    }

    [Fact]
    public async Task Create_With_Inapplicable_Field_Returns_BadRequest()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);
        var request = NewAsset(AssetCategory.Car, "Daily Driver") with { MaxPsi = 3000m };

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/assets", request, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("maxPsi", body);
    }

    [Fact]
    public async Task Create_Without_UsageTrackingMode_Applies_Registry_Default()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);

        var carResponse = await CreateAssetAsync(client, householdId, NewAsset(AssetCategory.Car, "Daily Driver"));
        var trailerResponse = await CreateAssetAsync(client, householdId, NewAsset(AssetCategory.UtilityTrailer, "Hauler"));

        Assert.Equal(UsageTrackingMode.Mileage, carResponse.UsageTrackingMode);
        Assert.Equal(UsageTrackingMode.None, trailerResponse.UsageTrackingMode);
    }

    [Fact]
    public async Task Create_With_Explicit_UsageTrackingMode_Overrides_Default()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);
        var request = NewAsset(AssetCategory.Car, "Track Car") with { UsageTrackingMode = UsageTrackingMode.Both };

        var created = await CreateAssetAsync(client, householdId, request);

        Assert.Equal(UsageTrackingMode.Both, created.UsageTrackingMode);
    }

    [Fact]
    public async Task Viewer_Cannot_Create_Asset()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Viewer);
        var client = CreateAuthenticatedClient(userId);
        var request = NewAsset(AssetCategory.Snowmobile, "Ski-Doo");

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/assets", request, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NonMember_Cannot_Create_Asset()
    {
        var householdId = await CreateHouseholdAsync();
        var client = CreateAuthenticatedClient(Guid.NewGuid());
        var request = NewAsset(AssetCategory.Snowmobile, "Ski-Doo");

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/assets", request, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_With_Unknown_Category_Returns_BadRequest()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync($"/api/households/{householdId}/assets", new { category = "Spaceship", name = "Rocket" }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Member_Can_List_And_Filter_Assets_By_Category()
    {
        var (householdId, ownerId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(ownerId);

        await CreateAssetAsync(client, householdId, NewAsset(AssetCategory.Snowmobile, "Ski-Doo"));
        await CreateAssetAsync(client, householdId, NewAsset(AssetCategory.PowerBoat, "Sea Ray") with { Hin = "ABC12345D404" });

        var listResponse = await client.GetAsync($"/api/households/{householdId}/assets", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var all = await listResponse.Content.ReadFromJsonAsync<List<AssetResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(2, all!.Count);

        var filteredResponse = await client.GetAsync($"/api/households/{householdId}/assets?category=PowerBoat", TestContext.Current.CancellationToken);
        var filtered = await filteredResponse.Content.ReadFromJsonAsync<List<AssetResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(filtered!);
        Assert.Equal(AssetCategory.PowerBoat, filtered![0].Category);
    }

    [Fact]
    public async Task Member_Can_Filter_Assets_By_Group()
    {
        var (householdId, ownerId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(ownerId);

        await CreateAssetAsync(client, householdId, NewAsset(AssetCategory.Utv, "Ranger"));
        await CreateAssetAsync(client, householdId, NewAsset(AssetCategory.Snowmobile, "Ski-Doo"));
        await CreateAssetAsync(client, householdId, NewAsset(AssetCategory.Car, "Daily Driver"));

        var response = await client.GetAsync($"/api/households/{householdId}/assets?group=Powersport", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var filtered = await response.Content.ReadFromJsonAsync<List<AssetResponse>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(2, filtered!.Count);
        Assert.All(filtered, a => Assert.NotEqual(AssetCategory.Car, a.Category));
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
        var created = await CreateAssetAsync(client, householdId, NewAsset(AssetCategory.Snowmobile, "Ski-Doo"));

        var updateRequest = NewUpdate("Ski-Doo Renamed");

        var response = await client.PutAsJsonAsync($"/api/households/{householdId}/assets/{created.Id}", updateRequest, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<AssetResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("Ski-Doo Renamed", updated!.Name);
    }

    [Fact]
    public async Task Update_With_Mismatched_Category_Returns_BadRequest()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);
        var created = await CreateAssetAsync(client, householdId, NewAsset(AssetCategory.Snowmobile, "Ski-Doo"));

        var updateRequest = NewUpdate("Ski-Doo") with { Category = AssetCategory.PowerBoat };

        var response = await client.PutAsJsonAsync($"/api/households/{householdId}/assets/{created.Id}", updateRequest, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_With_Inapplicable_Field_Returns_BadRequest()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);
        var created = await CreateAssetAsync(client, householdId, NewAsset(AssetCategory.EnclosedTrailer, "Cargo Trailer"));

        var updateRequest = NewUpdate("Cargo Trailer") with { Vin = "1FTSW21P34EB12345" };

        var response = await client.PutAsJsonAsync($"/api/households/{householdId}/assets/{created.Id}", updateRequest, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("vin", body);
    }

    [Fact]
    public async Task Viewer_Cannot_Update_Asset()
    {
        var (householdId, ownerId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var ownerClient = CreateAuthenticatedClient(ownerId);
        var created = await CreateAssetAsync(ownerClient, householdId, NewAsset(AssetCategory.Snowmobile, "Ski-Doo"));

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
        var created = await CreateAssetAsync(client, householdId, NewAsset(AssetCategory.Snowmobile, "Ski-Doo"));

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
        var created = await CreateAssetAsync(ownerClient, householdId, NewAsset(AssetCategory.Snowmobile, "Ski-Doo"));

        var contributorId = Guid.NewGuid();
        await AddMemberAsync(householdId, contributorId, HouseholdMemberRole.Contributor);
        var contributorClient = CreateAuthenticatedClient(contributorId);

        var response = await contributorClient.DeleteAsync($"/api/households/{householdId}/assets/{created.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static CreateAssetRequest NewAsset(AssetCategory category, string name) => new(
        Category: category,
        Name: name,
        Description: null,
        Year: null,
        UsageTrackingMode: null,
        Vin: null,
        Make: null,
        Model: null,
        Color: null,
        TrackLengthIn: null,
        Hin: null,
        HullMaterial: null,
        HullType: null,
        DriveType: null,
        KeelType: null,
        MastHeightFt: null,
        MastCount: null,
        LengthFt: null,
        BeamFt: null,
        BallSizeIn: null,
        MaxLoadLbs: null,
        InteriorHeightFt: null,
        InteriorLengthFt: null,
        CuttingWidthIn: null,
        MaxPsi: null,
        MaxGpm: null,
        EquipmentDescription: null, LicensePlate: null);

    private static UpdateAssetRequest NewUpdate(string name) => new(
        Category: null,
        Name: name,
        Description: null,
        Year: null,
        UsageTrackingMode: UsageTrackingMode.None,
        Vin: null,
        Make: null,
        Model: null,
        Color: null,
        TrackLengthIn: null,
        Hin: null,
        HullMaterial: null,
        HullType: null,
        DriveType: null,
        KeelType: null,
        MastHeightFt: null,
        MastCount: null,
        LengthFt: null,
        BeamFt: null,
        BallSizeIn: null,
        MaxLoadLbs: null,
        InteriorHeightFt: null,
        InteriorLengthFt: null,
        CuttingWidthIn: null,
        MaxPsi: null,
        MaxGpm: null,
        EquipmentDescription: null, LicensePlate: null);

    private static async Task<AssetResponse> CreateAssetAsync(
        HttpClient client, Guid householdId, CreateAssetRequest request)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets", request, TestJson.Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AssetResponse>(TestJson.Options))!;
    }
}
