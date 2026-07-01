using System.Net;
using System.Net.Http.Json;
using Steward.Application.Tracking.Registrations;
using Steward.Domain.Enums;
using Steward.IntegrationTests.Infrastructure;

namespace Steward.IntegrationTests.Tracking;

public class RegistrationDocumentControllerTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Upload_Download_And_Delete_Document_Round_Trip()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);
        var registrationId = await CreateRegistrationAsync(client, householdId, assetId);

        var pdfBytes = "%PDF-1.4 fake content"u8.ToArray();
        var uploadResponse = await UploadAsync(client, householdId, assetId, registrationId, pdfBytes, "application/pdf");
        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<RegistrationResponse>(TestJson.Options);
        Assert.True(uploaded!.HasDocument);
        Assert.NotNull(uploaded.DocumentUrl);

        var downloadResponse = await client.GetAsync(
            $"/api/households/{householdId}/assets/{assetId}/registrations/{registrationId}/document");
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        Assert.Equal("application/pdf", downloadResponse.Content.Headers.ContentType?.MediaType);
        var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        Assert.Equal(pdfBytes, downloadedBytes);

        var deleteResponse = await client.DeleteAsync(
            $"/api/households/{householdId}/assets/{assetId}/registrations/{registrationId}/document");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var afterDeleteResponse = await client.GetAsync(
            $"/api/households/{householdId}/assets/{assetId}/registrations/{registrationId}/document");
        Assert.Equal(HttpStatusCode.NotFound, afterDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task Unsupported_Content_Type_Rejected()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);
        var registrationId = await CreateRegistrationAsync(client, householdId, assetId);

        var response = await UploadAsync(
            client, householdId, assetId, registrationId, "not a real zip"u8.ToArray(), "application/zip");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Oversized_File_Rejected()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);
        var registrationId = await CreateRegistrationAsync(client, householdId, assetId);

        var oversizedBytes = new byte[11 * 1024 * 1024];
        var response = await UploadAsync(client, householdId, assetId, registrationId, oversizedBytes, "application/pdf");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_Cannot_Upload_Document()
    {
        var (householdId, ownerId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var ownerClient = CreateAuthenticatedClient(ownerId);
        var registrationId = await CreateRegistrationAsync(ownerClient, householdId, assetId);

        var viewerId = Guid.NewGuid();
        await AddMemberAsync(householdId, viewerId, HouseholdMemberRole.Viewer);
        var viewerClient = CreateAuthenticatedClient(viewerId);

        var response = await UploadAsync(
            viewerClient, householdId, assetId, registrationId, "%PDF-1.4"u8.ToArray(), "application/pdf");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Reupload_Replaces_Prior_Document()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);
        var registrationId = await CreateRegistrationAsync(client, householdId, assetId);

        var firstBytes = "first document"u8.ToArray();
        await UploadAsync(client, householdId, assetId, registrationId, firstBytes, "application/pdf");

        var rootPath = Environment.GetEnvironmentVariable("Storage__RootPath")!;
        var entityDir = Path.Combine(rootPath, "registrations", registrationId.ToString());
        Assert.Single(Directory.GetFiles(entityDir));

        var secondBytes = "second document"u8.ToArray();
        var secondUploadResponse = await UploadAsync(
            client, householdId, assetId, registrationId, secondBytes, "application/pdf");
        Assert.Equal(HttpStatusCode.OK, secondUploadResponse.StatusCode);

        Assert.Single(Directory.GetFiles(entityDir));

        var downloadResponse = await client.GetAsync(
            $"/api/households/{householdId}/assets/{assetId}/registrations/{registrationId}/document");
        var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        Assert.Equal(secondBytes, downloadedBytes);
    }

    private static async Task<HttpResponseMessage> UploadAsync(
        HttpClient client, Guid householdId, Guid assetId, Guid registrationId, byte[] content, string contentType)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(content);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        form.Add(fileContent, "file", "document");

        return await client.PostAsync(
            $"/api/households/{householdId}/assets/{assetId}/registrations/{registrationId}/document", form);
    }

    private static async Task<Guid> CreateRegistrationAsync(HttpClient client, Guid householdId, Guid assetId)
    {
        var createResponse = await client.PostAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/registrations",
            new CreateRegistrationRequest(
                RegistrationNumber: "ABC-1234",
                IssuingAuthority: "DMV",
                RenewedOn: new DateOnly(2026, 1, 15),
                Cost: 120.00m,
                ExpiresOn: new DateOnly(2027, 1, 15),
                Notes: null),
            TestJson.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<RegistrationResponse>(TestJson.Options);
        return created!.Id;
    }
}
