namespace Steward.Domain.Entities;

public class ServiceRecord
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public Guid? EngineId { get; set; }
    public DateOnly Date { get; set; }
    public required string Description { get; set; }
    public string? ProviderName { get; set; }
    public decimal? Cost { get; set; }
    public decimal? OdometerMiles { get; set; }
    public decimal? EngineHours { get; set; }
    public string? Notes { get; set; }
}
