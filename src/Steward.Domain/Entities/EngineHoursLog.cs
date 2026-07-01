namespace Steward.Domain.Entities;

public class EngineHoursLog
{
    public Guid Id { get; set; }
    public Guid EngineId { get; set; }
    public DateOnly Date { get; set; }
    public decimal? HoursReading { get; set; }
    public decimal? TripHours { get; set; }
    public string? Notes { get; set; }
}
