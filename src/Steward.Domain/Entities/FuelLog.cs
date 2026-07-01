using Steward.Domain.Enums;

namespace Steward.Domain.Entities;

public class FuelLog
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public Guid? EngineId { get; set; }
    public FuelLogType LogType { get; set; }
    public DateOnly Date { get; set; }
    public decimal Volume { get; set; }
    public VolumeUnit VolumeUnit { get; set; }
    public string? FuelGrade { get; set; }
    public decimal? PricePerUnit { get; set; }
    public decimal? TotalCost { get; set; }
    public decimal? MilesAtLog { get; set; }
    public decimal? HoursAtLog { get; set; }
    public string? Notes { get; set; }
}
