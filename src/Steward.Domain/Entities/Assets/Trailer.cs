namespace Steward.Domain.Entities.Assets;

public class Trailer : Asset
{
    public decimal? BallSizeIn { get; set; }
    public decimal? MaxLoadLbs { get; set; }
    public decimal? InteriorHeightFt { get; set; }
    public decimal? InteriorLengthFt { get; set; }
    public string? LicensePlate { get; set; }
}
