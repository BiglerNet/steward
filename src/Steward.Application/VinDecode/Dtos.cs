namespace Steward.Application.VinDecode;

public record VinDecodeResult(
    string Vin,
    string? Make,
    string? Model,
    int? ModelYear,
    string? BodyClass,
    string? VehicleType,
    string? FuelTypePrimary,
    int? EngineCylinders,
    decimal? DisplacementLiters);
