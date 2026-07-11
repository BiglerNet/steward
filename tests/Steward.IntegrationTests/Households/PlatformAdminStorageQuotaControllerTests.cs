using System.Net;
using System.Net.Http.Json;
using Steward.Application.Households;
using Steward.Application.PlatformAdmin;
using Steward.Domain.Enums;
using Steward.IntegrationTests.Infrastructure;

namespace Steward.IntegrationTests.Households;

public class PlatformAdminStorageQuotaControllerTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task PlatformAdmin_Raises_Households_Quota()
    {
        var (householdId, memberId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var adminClient = CreateAuthenticatedClient(Guid.NewGuid(), roles: ["PlatformAdmin"]);

        var response = await adminClient.PutAsJsonAsync(
            $"/api/admin/households/{householdId}/storage-quota",
            new SetStorageQuotaRequest(5_368_709_120),
            TestJson.Options,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var memberClient = CreateAuthenticatedClient(memberId);
        var householdResponse = await memberClient.GetAsync($"/api/households/{householdId}", TestContext.Current.CancellationToken);
        var household = await householdResponse.Content.ReadFromJsonAsync<HouseholdResponse>(TestJson.Options, TestContext.Current.CancellationToken);

        Assert.Equal(5_368_709_120, household!.StorageQuotaBytes);
    }

    [Fact]
    public async Task Clearing_The_Override_Restores_The_Default()
    {
        var (householdId, memberId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var adminClient = CreateAuthenticatedClient(Guid.NewGuid(), roles: ["PlatformAdmin"]);

        await adminClient.PutAsJsonAsync(
            $"/api/admin/households/{householdId}/storage-quota",
            new SetStorageQuotaRequest(5_368_709_120),
            TestJson.Options,
            TestContext.Current.CancellationToken);

        var clearResponse = await adminClient.PutAsJsonAsync(
            $"/api/admin/households/{householdId}/storage-quota",
            new SetStorageQuotaRequest(null),
            TestJson.Options,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, clearResponse.StatusCode);

        var memberClient = CreateAuthenticatedClient(memberId);
        var householdResponse = await memberClient.GetAsync($"/api/households/{householdId}", TestContext.Current.CancellationToken);
        var household = await householdResponse.Content.ReadFromJsonAsync<HouseholdResponse>(TestJson.Options, TestContext.Current.CancellationToken);

        Assert.Equal(1024L * 1024 * 1024, household!.StorageQuotaBytes);
    }

    [Fact]
    public async Task Non_Positive_Quota_Is_Rejected()
    {
        var (householdId, _) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var adminClient = CreateAuthenticatedClient(Guid.NewGuid(), roles: ["PlatformAdmin"]);

        var response = await adminClient.PutAsJsonAsync(
            $"/api/admin/households/{householdId}/storage-quota",
            new SetStorageQuotaRequest(0),
            TestJson.Options,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Household_Owner_Without_PlatformAdmin_Role_Cannot_Change_Quota()
    {
        var (householdId, ownerId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var ownerClient = CreateAuthenticatedClient(ownerId);

        var response = await ownerClient.PutAsJsonAsync(
            $"/api/admin/households/{householdId}/storage-quota",
            new SetStorageQuotaRequest(5_368_709_120),
            TestJson.Options,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Member_Sees_Storage_Usage_And_Quota_On_Household_Detail()
    {
        var (householdId, userId) = await CreateHouseholdWithMemberAsync(HouseholdMemberRole.Owner);
        var client = CreateAuthenticatedClient(userId);

        var response = await client.GetAsync($"/api/households/{householdId}", TestContext.Current.CancellationToken);
        var household = await response.Content.ReadFromJsonAsync<HouseholdResponse>(TestJson.Options, TestContext.Current.CancellationToken);

        Assert.Equal(0, household!.StorageUsedBytes);
        Assert.Equal(1024L * 1024 * 1024, household.StorageQuotaBytes);
    }
}
