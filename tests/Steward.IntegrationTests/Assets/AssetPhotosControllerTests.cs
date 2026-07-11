using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Steward.Application.Photos;
using Steward.Domain.Enums;
using Steward.IntegrationTests.Infrastructure;
using SkiaSharp;

namespace Steward.IntegrationTests.Assets;

public class AssetPhotosControllerTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Upload_Returns_201_And_Becomes_Cover_When_First_Photo()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var response = await UploadAsync(client, householdId, assetId, CreateJpeg());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var photo = await response.Content.ReadFromJsonAsync<AssetPhotoResponse>(TestJson.Options, TestContext.Current.CancellationToken);
        Assert.NotNull(photo);

        var assetResponse = await client.GetAsync(
            $"/api/households/{householdId}/assets/{assetId}", TestContext.Current.CancellationToken);
        var asset = await assetResponse.Content.ReadFromJsonAsync<AssetResponse>(TestJson.Options, TestContext.Current.CancellationToken);
        Assert.Equal(photo!.Id, asset!.CoverPhotoId);
    }

    [Fact]
    public async Task Get_Content_Streams_Requested_Variant_And_Rejects_Unknown_Variant()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var uploadResponse = await UploadAsync(client, householdId, assetId, CreateJpeg());
        var photo = await uploadResponse.Content.ReadFromJsonAsync<AssetPhotoResponse>(TestJson.Options, TestContext.Current.CancellationToken);

        var displayResponse = await client.GetAsync(
            $"/api/households/{householdId}/assets/{assetId}/photos/{photo!.Id}/content?variant=display",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, displayResponse.StatusCode);
        Assert.Equal("image/jpeg", displayResponse.Content.Headers.ContentType?.MediaType);

        var invalidResponse = await client.GetAsync(
            $"/api/households/{householdId}/assets/{assetId}/photos/{photo.Id}/content?variant=original",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
    }

    [Fact]
    public async Task Deleting_The_Cover_Reassigns_To_Newest_Remaining_Photo()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var firstResponse = await UploadAsync(client, householdId, assetId, CreateJpeg());
        var first = await firstResponse.Content.ReadFromJsonAsync<AssetPhotoResponse>(TestJson.Options, TestContext.Current.CancellationToken);

        var secondResponse = await UploadAsync(client, householdId, assetId, CreateJpeg());
        var second = await secondResponse.Content.ReadFromJsonAsync<AssetPhotoResponse>(TestJson.Options, TestContext.Current.CancellationToken);

        var deleteResponse = await client.DeleteAsync(
            $"/api/households/{householdId}/assets/{assetId}/photos/{first!.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var assetResponse = await client.GetAsync(
            $"/api/households/{householdId}/assets/{assetId}", TestContext.Current.CancellationToken);
        var asset = await assetResponse.Content.ReadFromJsonAsync<AssetResponse>(TestJson.Options, TestContext.Current.CancellationToken);
        Assert.Equal(second!.Id, asset!.CoverPhotoId);
    }

    [Fact]
    public async Task Deleting_The_Only_Photo_Clears_The_Cover()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var uploadResponse = await UploadAsync(client, householdId, assetId, CreateJpeg());
        var photo = await uploadResponse.Content.ReadFromJsonAsync<AssetPhotoResponse>(TestJson.Options, TestContext.Current.CancellationToken);

        await client.DeleteAsync(
            $"/api/households/{householdId}/assets/{assetId}/photos/{photo!.Id}", TestContext.Current.CancellationToken);

        var assetResponse = await client.GetAsync(
            $"/api/households/{householdId}/assets/{assetId}", TestContext.Current.CancellationToken);
        var asset = await assetResponse.Content.ReadFromJsonAsync<AssetResponse>(TestJson.Options, TestContext.Current.CancellationToken);
        Assert.Null(asset!.CoverPhotoId);
    }

    [Fact]
    public async Task Setting_Cover_To_A_Foreign_Photo_Is_Rejected()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var otherAssetId = await CreateAssetAsync(householdId, "Other Asset");
        var client = CreateAuthenticatedClient(userId);

        var foreignUploadResponse = await UploadAsync(client, householdId, otherAssetId, CreateJpeg());
        var foreignPhoto = await foreignUploadResponse.Content.ReadFromJsonAsync<AssetPhotoResponse>(TestJson.Options, TestContext.Current.CancellationToken);

        var response = await client.PutAsJsonAsync(
            $"/api/households/{householdId}/assets/{assetId}/cover-photo",
            new SetCoverPhotoRequest(foreignPhoto!.Id),
            TestJson.Options,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_Cannot_Upload_Or_Delete_Photos()
    {
        var (householdId, ownerId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var assetId = await CreateAssetAsync(householdId);
        var ownerClient = CreateAuthenticatedClient(ownerId);

        var uploadResponse = await UploadAsync(ownerClient, householdId, assetId, CreateJpeg());
        var photo = await uploadResponse.Content.ReadFromJsonAsync<AssetPhotoResponse>(TestJson.Options, TestContext.Current.CancellationToken);

        var viewerId = Guid.NewGuid();
        await AddMemberAsync(householdId, viewerId, HouseholdMemberRole.Viewer);
        var viewerClient = CreateAuthenticatedClient(viewerId);

        var viewerUploadResponse = await UploadAsync(viewerClient, householdId, assetId, CreateJpeg());
        Assert.Equal(HttpStatusCode.Forbidden, viewerUploadResponse.StatusCode);

        var viewerDeleteResponse = await viewerClient.DeleteAsync(
            $"/api/households/{householdId}/assets/{assetId}/photos/{photo!.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, viewerDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task Upload_Over_Household_Quota_Is_Rejected_And_Usage_Unchanged()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        await SetHouseholdStorageQuotaOverrideAsync(householdId, quotaBytes: 10);

        var response = await UploadAsync(client, householdId, assetId, CreateJpeg());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await GetHouseholdStorageUsedBytesAsync(householdId));
    }

    [Fact]
    public async Task Upload_Increases_Usage_By_Stored_Variant_Bytes_Not_Upload_Size()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Contributor);
        var assetId = await CreateAssetAsync(householdId);
        var client = CreateAuthenticatedClient(userId);

        var uploadResponse = await UploadAsync(client, householdId, assetId, CreateJpeg(width: 3000, height: 2000));
        var photo = await uploadResponse.Content.ReadFromJsonAsync<AssetPhotoResponse>(TestJson.Options, TestContext.Current.CancellationToken);

        var usedBytes = await GetHouseholdStorageUsedBytesAsync(householdId);
        Assert.Equal(photo!.SizeBytes, usedBytes);
    }

    private static byte[] CreateJpeg(int width = 200, int height = 150)
    {
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.CornflowerBlue);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        return encoded.ToArray();
    }

    private static async Task<HttpResponseMessage> UploadAsync(
        HttpClient client, Guid householdId, Guid assetId, byte[] content)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(fileContent, "file", "photo.jpg");

        return await client.PostAsync($"/api/households/{householdId}/assets/{assetId}/photos", form);
    }

    private record AssetResponse(Guid Id, Guid HouseholdId, Guid? CoverPhotoId);
}
