using Steward.Domain.Enums;

namespace Steward.Domain.Entities;

public class DashboardWidget
{
    public Guid Id { get; set; }
    public Guid DashboardId { get; set; }
    public WidgetType WidgetType { get; set; }
    public WidgetSize WidgetSize { get; set; }
    public int Position { get; set; }
    public string? Config { get; set; }
    public HouseholdDashboard Dashboard { get; set; } = null!;
}
