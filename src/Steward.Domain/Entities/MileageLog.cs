namespace Steward.Domain.Entities;

public class MileageLog
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public DateOnly Date { get; set; }
    public decimal? OdometerReading { get; set; }
    public decimal? TripMiles { get; set; }
    public string? Notes { get; set; }
}
