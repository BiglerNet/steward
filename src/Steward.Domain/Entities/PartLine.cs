using Steward.Domain.Enums;

namespace Steward.Domain.Entities;

public class PartLine
{
    public Guid Id { get; set; }
    public Guid MaintenanceItemId { get; set; }
    public required string Name { get; set; }
    public string? PartNumber { get; set; }
    public string? Vendor { get; set; }
    public string? TrackingNumber { get; set; }
    public string? OrderUrl { get; set; }
    public decimal Quantity { get; set; } = 1;
    public PartLineStatus Status { get; set; } = PartLineStatus.Needed;
    public decimal? Cost { get; set; }
    public Guid? ChecklistItemId { get; set; }
    public Guid? PartId { get; set; }
}
