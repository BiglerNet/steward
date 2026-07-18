using Steward.Domain.Enums;

namespace Steward.Domain.Entities;

public class MaintenanceItem
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public Guid? EngineId { get; set; }
    public Guid? TemplateId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? ProviderName { get; set; }
    public MaintenanceItemStatus Status { get; set; } = MaintenanceItemStatus.Planned;
    public DateOnly? Date { get; set; }
    public decimal? Cost { get; set; }
    public decimal? OdometerMiles { get; set; }
    public decimal? EngineHours { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public List<ChecklistItem> ChecklistItems { get; set; } = [];
    public List<PartLine> PartLines { get; set; } = [];
}
