namespace Steward.Domain.Entities.Assets;

public class Vehicle : Asset
{
    public string? Vin { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? Color { get; set; }
    public decimal? TrackLengthIn { get; set; }
    public string? LicensePlate { get; set; }
}
