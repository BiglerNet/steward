using System.Net;
using System.Net.Http.Json;
using Steward.Application.Households;
using Steward.Infrastructure.Identity;
using Steward.Infrastructure.Persistence;
using Steward.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Steward.IntegrationTests.Households;

public class HouseholdsControllerTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Create_With_Location_Succeeds()
    {
        var userId = await CreateUserAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync("/api/households", NewCreateRequest() with
        {
            Country = "US",
            Region = "US-WI",
        }, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var household = await response.Content.ReadFromJsonAsync<HouseholdResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("US", household!.Country);
        Assert.Equal("US-WI", household.Region);
    }

    [Fact]
    public async Task Update_Can_Set_And_Clear_Location()
    {
        var userId = await CreateUserAsync();
        var client = CreateAuthenticatedClient(userId);

        var createResponse = await client.PostAsJsonAsync("/api/households", NewCreateRequest(), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<HouseholdResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        var setResponse = await client.PutAsJsonAsync($"/api/households/{created!.Id}", NewUpdateRequest(created) with
        {
            Country = "CA",
            Region = "CA-ON",
        }, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, setResponse.StatusCode);
        var setResult = await setResponse.Content.ReadFromJsonAsync<HouseholdResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("CA", setResult!.Country);
        Assert.Equal("CA-ON", setResult.Region);

        var clearResponse = await client.PutAsJsonAsync($"/api/households/{created.Id}", NewUpdateRequest(created), TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, clearResponse.StatusCode);
        var clearResult = await clearResponse.Content.ReadFromJsonAsync<HouseholdResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Null(clearResult!.Country);
        Assert.Null(clearResult.Region);
    }

    [Fact]
    public async Task Create_With_Mismatched_Region_Returns_BadRequest()
    {
        var userId = await CreateUserAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync("/api/households", NewCreateRequest() with
        {
            Country = "US",
            Region = "CA-ON",
        }, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_With_Region_Without_Country_Returns_BadRequest()
    {
        var userId = await CreateUserAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync("/api/households", NewCreateRequest() with
        {
            Country = null,
            Region = "US-WI",
        }, TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<Guid> CreateUserAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StewardDbContext>();

        var userId = Guid.NewGuid();
        dbContext.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = $"test-{Guid.NewGuid():N}@example.com",
            NormalizedUserName = $"TEST-{Guid.NewGuid():N}@EXAMPLE.COM",
            Email = $"test-{Guid.NewGuid():N}@example.com",
        });

        await dbContext.SaveChangesAsync();
        return userId;
    }

    private static CreateHouseholdRequest NewCreateRequest() => new(
        Name: "Test Household",
        PublicSlug: $"test-{Guid.NewGuid():N}",
        IsPublicVisible: false,
        Country: null,
        Region: null);

    private static UpdateHouseholdRequest NewUpdateRequest(HouseholdResponse household) => new(
        Name: household.Name,
        PublicSlug: household.PublicSlug,
        IsPublicVisible: household.IsPublicVisible,
        Country: null,
        Region: null);
}
