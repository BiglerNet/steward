using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Steward.Application.Auth;
using Steward.IntegrationTests.Infrastructure;

namespace Steward.IntegrationTests.Auth;

public class AuthControllerOAuthProvidersTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task No_Providers_Configured_Returns_All_False()
    {
        var client = CreateAnonymousClient();

        var response = await client.GetAsync("/api/auth/oauth/providers", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OAuthProvidersResponse>(
            TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(new OAuthProvidersResponse(false, false, false), body);
    }

    [Fact]
    public async Task All_Providers_Configured_Returns_All_True()
    {
        using var factory = Factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration(
            (_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Google:ClientId"] = "google-client-id",
                ["Auth:Facebook:ClientId"] = "facebook-client-id",
                ["Auth:Apple:ClientId"] = "apple-client-id",
            })));
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/oauth/providers", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OAuthProvidersResponse>(
            TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(new OAuthProvidersResponse(true, true, true), body);
    }

    [Fact]
    public async Task Mixed_Configuration_Reports_Only_Configured_Providers()
    {
        using var factory = Factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration(
            (_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Google:ClientId"] = "google-client-id",
                ["Auth:Facebook:ClientId"] = "",
                ["Auth:Apple:ClientId"] = "",
            })));
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/oauth/providers", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OAuthProvidersResponse>(
            TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(new OAuthProvidersResponse(true, false, false), body);
    }

    [Fact]
    public async Task Response_Does_Not_Leak_Secret_Values()
    {
        using var factory = Factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration(
            (_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Google:ClientId"] = "google-client-id",
                ["Auth:Google:ClientSecret"] = "super-secret-value",
            })));
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/oauth/providers", TestContext.Current.CancellationToken);

        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("super-secret-value", raw);
        Assert.DoesNotContain("google-client-id", raw);
    }
}
