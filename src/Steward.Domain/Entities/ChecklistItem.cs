using Steward.Domain.Enums;

namespace Steward.Domain.Entities;

public class ChecklistItem
{
    public Guid Id { get; set; }
    public Guid MaintenanceItemId { get; set; }
    public required string Text { get; set; }
    public ChecklistItemStatus Status { get; set; } = ChecklistItemStatus.Open;
    public DateTimeOffset? ResolvedAt { get; set; }
    public int SortOrder { get; set; }
    public Guid? EngineId { get; set; }
    public Guid? TemplateStepId { get; set; }
}
