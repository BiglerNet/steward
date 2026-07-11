using System.Net;
using Steward.Domain.Common.Exceptions;
using Steward.Infrastructure.VinDecode;

namespace Steward.UnitTests.VinDecode;

public class VpicVinDecodeServiceTests
{
    private const string Vin = "1HGCM82633A004352";

    [Fact]
    public async Task DecodeAsync_Maps_Populated_Fields()
    {
        var json = """
            {
                "Results": [
                    {
                        "Make": "HONDA",
                        "Model": "Accord",
                        "ModelYear": "2003",
                        "BodyClass": "Sedan/Saloon",
                        "VehicleType": "PASSENGER CAR",
                        "FuelTypePrimary": "Gasoline",
                        "EngineCylinders": "4",
                        "DisplacementL": "2.4"
                    }
                ]
            }
            """;
        var service = CreateService(json);

        var result = await service.DecodeAsync(Vin, TestContext.Current.CancellationToken);

        Assert.Equal(Vin, result.Vin);
        Assert.Equal("HONDA", result.Make);
        Assert.Equal("Accord", result.Model);
        Assert.Equal(2003, result.ModelYear);
        Assert.Equal("Sedan/Saloon", result.BodyClass);
        Assert.Equal("PASSENGER CAR", result.VehicleType);
        Assert.Equal("Gasoline", result.FuelTypePrimary);
        Assert.Equal(4, result.EngineCylinders);
        Assert.Equal(2.4m, result.DisplacementLiters);
    }

    [Fact]
    public async Task DecodeAsync_Maps_Empty_Strings_To_Null()
    {
        var json = """
            {
                "Results": [
                    {
                        "Make": "",
                        "Model": "",
                        "ModelYear": "",
                        "BodyClass": "",
                        "VehicleType": "",
                        "FuelTypePrimary": "",
                        "EngineCylinders": "",
                        "DisplacementL": ""
                    }
                ]
            }
            """;
        var service = CreateService(json);

        var result = await service.DecodeAsync(Vin, TestContext.Current.CancellationToken);

        Assert.Equal(Vin, result.Vin);
        Assert.Null(result.Make);
        Assert.Null(result.Model);
        Assert.Null(result.ModelYear);
        Assert.Null(result.BodyClass);
        Assert.Null(result.VehicleType);
        Assert.Null(result.FuelTypePrimary);
        Assert.Null(result.EngineCylinders);
        Assert.Null(result.DisplacementLiters);
    }

    [Fact]
    public async Task DecodeAsync_Maps_Malformed_Numerics_To_Null()
    {
        var json = """
            {
                "Results": [
                    {
                        "Make": "HONDA",
                        "ModelYear": "not-a-year",
                        "EngineCylinders": "four",
                        "DisplacementL": "n/a"
                    }
                ]
            }
            """;
        var service = CreateService(json);

        var result = await service.DecodeAsync(Vin, TestContext.Current.CancellationToken);

        Assert.Equal("HONDA", result.Make);
        Assert.Null(result.ModelYear);
        Assert.Null(result.EngineCylinders);
        Assert.Null(result.DisplacementLiters);
    }

    [Fact]
    public async Task DecodeAsync_Returns_Null_Fields_When_No_Results()
    {
        var service = CreateService("""{ "Results": [] }""");

        var result = await service.DecodeAsync(Vin, TestContext.Current.CancellationToken);

        Assert.Equal(Vin, result.Vin);
        Assert.Null(result.Make);
        Assert.Null(result.Model);
    }

    [Fact]
    public async Task DecodeAsync_Throws_BadGatewayException_On_Upstream_Failure()
    {
        var service = CreateService(responseStatusCode: HttpStatusCode.ServiceUnavailable);

        await Assert.ThrowsAsync<BadGatewayException>(
            () => service.DecodeAsync(Vin, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DecodeAsync_Throws_BadGatewayException_On_Timeout()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new TaskCanceledException("timed out"));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://vpic.nhtsa.dot.gov/api/vehicles/") };
        var service = new VpicVinDecodeService(httpClient);

        await Assert.ThrowsAsync<BadGatewayException>(
            () => service.DecodeAsync(Vin, TestContext.Current.CancellationToken));
    }

    private static VpicVinDecodeService CreateService(
        string? jsonBody = null, HttpStatusCode responseStatusCode = HttpStatusCode.OK)
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(responseStatusCode)
        {
            Content = new StringContent(jsonBody ?? string.Empty),
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://vpic.nhtsa.dot.gov/api/vehicles/") };
        return new VpicVinDecodeService(httpClient);
    }

    private class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
