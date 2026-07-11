using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Steward.Application.VinDecode;
using Steward.Domain.Common.Exceptions;

namespace Steward.Infrastructure.VinDecode;

public class VpicVinDecodeService(HttpClient httpClient) : IVinDecodeService
{
    public async Task<VinDecodeResult> DecodeAsync(string vin, CancellationToken cancellationToken = default)
    {
        VpicResponse? response;
        try
        {
            response = await httpClient.GetFromJsonAsync<VpicResponse>(
                $"DecodeVinValues/{vin}?format=json", cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            throw new BadGatewayException("Unable to decode VIN via the upstream vehicle data provider.");
        }

        var result = response?.Results?.FirstOrDefault();
        if (result is null)
        {
            return new VinDecodeResult(vin, null, null, null, null, null, null, null, null);
        }

        return new VinDecodeResult(
            vin,
            NullIfEmpty(result.Make),
            NullIfEmpty(result.Model),
            ParseInt(result.ModelYear),
            NullIfEmpty(result.BodyClass),
            NullIfEmpty(result.VehicleType),
            NullIfEmpty(result.FuelTypePrimary),
            ParseInt(result.EngineCylinders),
            ParseDecimal(result.DisplacementL));
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static int? ParseInt(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

    private static decimal? ParseDecimal(string? value) =>
        decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

    internal class VpicResponse
    {
        [JsonPropertyName("Results")]
        public List<VpicResult>? Results { get; set; }
    }

    internal class VpicResult
    {
        [JsonPropertyName("Make")]
        public string? Make { get; set; }

        [JsonPropertyName("Model")]
        public string? Model { get; set; }

        [JsonPropertyName("ModelYear")]
        public string? ModelYear { get; set; }

        [JsonPropertyName("BodyClass")]
        public string? BodyClass { get; set; }

        [JsonPropertyName("VehicleType")]
        public string? VehicleType { get; set; }

        [JsonPropertyName("FuelTypePrimary")]
        public string? FuelTypePrimary { get; set; }

        [JsonPropertyName("EngineCylinders")]
        public string? EngineCylinders { get; set; }

        [JsonPropertyName("DisplacementL")]
        public string? DisplacementL { get; set; }
    }
}
