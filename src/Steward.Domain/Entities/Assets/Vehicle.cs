namespace Steward.Domain.Entities.Assets;

public abstract class Vehicle : Asset
{
    public string? Vin { get; set; }
    public string? Color { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
}
