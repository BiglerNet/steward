namespace Steward.Domain.Entities.Assets;

public class Equipment : Asset
{
    public decimal? CuttingWidthIn { get; set; }
    public decimal? MaxPsi { get; set; }
    public decimal? MaxGpm { get; set; }
    public string? EquipmentDescription { get; set; }
}
