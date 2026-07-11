using System.Net;
using System.Net.Http.Json;
using Steward.Application.Tracking.Registrations;
using Steward.Domain.Enums;
using Steward.IntegrationTests.Infrastructure;

namespace Steward.IntegrationTests.Storage;

public class StorageQuotaControllerTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Uploading_A_Document_Increases_Household_Storage_Usage()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);
        var registrationId = await CreateRegistrationAsync(client, householdId, assetId);

        var bytes = "%PDF-1.4 some document content"u8.ToArray();
        var response = await UploadAsync(client, householdId, assetId, registrationId, bytes);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var usedBytes = await GetHouseholdStorageUsedBytesAsync(householdId);
        Assert.Equal(bytes.Length, usedBytes);
    }

    [Fact]
    public async Task Deleting_A_Document_Decreases_Household_Storage_Usage()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);
        var registrationId = await CreateRegistrationAsync(client, householdId, assetId);

        await UploadAsync(client, householdId, assetId, registrationId, "%PDF-1.4 content"u8.ToArray());
        Assert.True(await GetHouseholdStorageUsedBytesAsync(householdId) > 0);

        var deleteResponse = await client.DeleteAsync(
            $"/api/households/{householdId}/assets/{assetId}/registrations/{registrationId}/document",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        Assert.Equal(0, await GetHouseholdStorageUsedBytesAsync(householdId));
    }

    [Fact]
    public async Task Replacing_A_Document_Adjusts_Usage_By_The_Difference()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);
        var registrationId = await CreateRegistrationAsync(client, householdId, assetId);

        var firstBytes = "%PDF-1.4 a longer initial document body"u8.ToArray();
        await UploadAsync(client, householdId, assetId, registrationId, firstBytes);
        Assert.Equal(firstBytes.Length, await GetHouseholdStorageUsedBytesAsync(householdId));

        var secondBytes = "%PDF-1.4 short"u8.ToArray();
        var replaceResponse = await UploadAsync(client, householdId, assetId, registrationId, secondBytes);
        Assert.Equal(HttpStatusCode.OK, replaceResponse.StatusCode);

        Assert.Equal(secondBytes.Length, await GetHouseholdStorageUsedBytesAsync(householdId));
    }

    [Fact]
    public async Task Upload_Over_Household_Quota_Is_Rejected_And_Stores_Nothing()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);
        var registrationId = await CreateRegistrationAsync(client, householdId, assetId);

        await SetHouseholdStorageQuotaOverrideAsync(householdId, quotaBytes: 10);

        var bytes = "%PDF-1.4 this document is bigger than the quota allows"u8.ToArray();
        var response = await UploadAsync(client, householdId, assetId, registrationId, bytes);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await GetHouseholdStorageUsedBytesAsync(householdId));
    }

    private static async Task<HttpResponseMessage> UploadAsync(
        HttpClient client, Guid householdId, Guid assetId, Guid registrationId, byte[] content)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(content);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "file", "document");

        return await client.PostAsync(
            $"/api/households/{householdId}/assets/{assetId}/registrations/{registrationId}/document", form);
    }

    private static async Task<Guid> CreateRegistrationAsync(HttpClient client, Guid householdId, Guid assetId)
    {
        var createResponse = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/registrations",
            new CreateRegistrationRequest(
                Kind: RegistrationKind.Registration,
                RegistrationNumber: "ABC-1234",
                IssuingAuthority: "DMV",
                ValidFrom: new DateOnly(2026, 1, 15),
                RenewedOn: new DateOnly(2026, 1, 15),
                Cost: 120.00m,
                ExpiresOn: new DateOnly(2027, 1, 15),
                Notes: null),
            TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<RegistrationResponse>(TestJson.Options);
        return created!.Id;
    }
}
