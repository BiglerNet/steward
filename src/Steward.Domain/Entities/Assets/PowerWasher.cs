namespace Steward.Domain.Entities.Assets;

public class PowerWasher : Equipment
{
    public decimal? MaxPsi { get; set; }
    public decimal? MaxGpm { get; set; }
}
