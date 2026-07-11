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

        var createResponse = await clientA.PostAsJsonAsync($"/api/households/{householdA}/assets", new CreateAssetRequest(
                Category: AssetCategory.Snowmobile,
                Name: "Ski-Doo",
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
                EquipmentDescription: null, LicensePlate: null), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        createResponse.EnsureSuccessStatusCode();
        var asset = (await createResponse.Content.ReadFromJsonAsync<AssetResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken))!;

        var (householdB, ownerB) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var clientB = CreateAuthenticatedClient(ownerB);

        var response = await clientB.GetAsync($"/api/households/{householdB}/assets/{asset.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
