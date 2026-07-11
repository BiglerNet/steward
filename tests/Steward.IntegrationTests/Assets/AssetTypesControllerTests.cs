using System.Net;
using System.Net.Http.Json;
using Steward.Application.AssetTypes;
using Steward.Domain.Enums;
using Steward.IntegrationTests.Infrastructure;

namespace Steward.IntegrationTests.Assets;

public class AssetTypesControllerTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Anonymous_Caller_Gets_Full_Registry()
    {
        var client = CreateAnonymousClient();

        var response = await client.GetAsync("/api/asset-types", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var entries = await response.Content.ReadFromJsonAsync<List<AssetTypeDefinition>>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(entries);
        Assert.Equal(Enum.GetValues<AssetCategory>().Length, entries.Count);
        Assert.All(entries, entry =>
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.DisplayLabel));
            Assert.False(string.IsNullOrWhiteSpace(entry.Icon));
            Assert.NotNull(entry.ApplicableFields);
            Assert.NotNull(entry.TypicalPermitKinds);
        });
    }
}
