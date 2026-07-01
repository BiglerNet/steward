using System.Net;
using System.Net.Http.Json;
using Steward.Application.Assets;
using Steward.Domain.Enums;
using Steward.IntegrationTests.Infrastructure;

namespace Steward.IntegrationTests.Assets;

public class AssetHouseholdIsolationTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task GetById_AssetBelongsToDifferentHousehold_Returns_NotFound()
    {
        var (householdA, ownerA) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var clientA = CreateAuthenticatedClient(ownerA);

        var createResponse = await clientA.PostAsJsonAsync(
            $"/api/households/{householdA}/assets",
            new CreateAssetRequest(
                AssetType: AssetType.Snowmobile,
                Name: "Ski-Doo",
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
                EquipmentDescription: null),
            TestJson.Options);
        createResponse.EnsureSuccessStatusCode();
        var asset = (await createResponse.Content.ReadFromJsonAsync<AssetResponse>(TestJson.Options))!;

        var (householdB, ownerB) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var clientB = CreateAuthenticatedClient(ownerB);

        var response = await clientB.GetAsync($"/api/households/{householdB}/assets/{asset.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
