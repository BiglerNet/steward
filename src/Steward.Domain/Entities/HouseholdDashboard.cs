namespace Steward.Domain.Entities;

public class HouseholdDashboard
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public required string Name { get; set; }
    public bool IsDefault { get; set; }
    public int Position { get; set; }
    public ICollection<DashboardWidget> Widgets { get; set; } = [];
}
