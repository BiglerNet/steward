namespace Steward.Domain.Entities.Assets;

public class Boat : Vehicle
{
    /// Hull Identification Number — boats use HIN instead of the inherited Vin.
    public string? Hin { get; set; }
    public string? HullMaterial { get; set; }
    public decimal? LengthFt { get; set; }
    public decimal? BeamFt { get; set; }
}
