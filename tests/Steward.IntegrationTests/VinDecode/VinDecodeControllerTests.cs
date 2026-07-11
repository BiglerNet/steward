using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Steward.Application.VinDecode;
using Steward.Infrastructure.VinDecode;
using Steward.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Steward.IntegrationTests.VinDecode;

public class VinDecodeControllerTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    private const string ValidVin = "1HGCM82633A004352";

    [Fact]
    public async Task Anonymous_Caller_Gets_401()
    {
        var client = CreateAnonymousClient();

        var response = await client.GetAsync($"/api/vin-decode/{ValidVin}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Malformed_Vin_Returns_400()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid());

        var response = await client.GetAsync("/api/vin-decode/too-short", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Successful_Decode_Returns_200_With_Populated_Fields()
    {
        var json = """
            {
                "Results": [
                    { "Make": "HONDA", "Model": "Accord", "ModelYear": "2003" }
                ]
            }
            """;
        using var factory = WithStubbedUpstream(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json),
        });
        var client = AuthenticatedClient(factory);

        var response = await client.GetAsync($"/api/vin-decode/{ValidVin}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<VinDecodeResult>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal("HONDA", result!.Make);
        Assert.Equal("Accord", result.Model);
        Assert.Equal(2003, result.ModelYear);
    }

    [Fact]
    public async Task Undecodable_Vin_Returns_200_With_Null_Fields()
    {
        using var factory = WithStubbedUpstream(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{ "Results": [] }"""),
        });
        var client = AuthenticatedClient(factory);

        var response = await client.GetAsync($"/api/vin-decode/{ValidVin}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<VinDecodeResult>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Null(result!.Make);
    }

    [Fact]
    public async Task Upstream_Failure_Returns_502()
    {
        using var factory = WithStubbedUpstream(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var client = AuthenticatedClient(factory);

        var response = await client.GetAsync($"/api/vin-decode/{ValidVin}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    private WebApplicationFactory<Program> WithStubbedUpstream(
        Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        return Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHttpMessageHandlerFactory>();
                services.AddHttpClient<IVinDecodeService, VpicVinDecodeService>(client =>
                {
                    client.BaseAddress = new Uri("https://vpic.nhtsa.dot.gov/api/vehicles/");
                    client.Timeout = TimeSpan.FromSeconds(8);
                }).ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler(respond));
            });
        });
    }

    private static HttpClient AuthenticatedClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var token = TestJwt.Create(Guid.NewGuid());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
