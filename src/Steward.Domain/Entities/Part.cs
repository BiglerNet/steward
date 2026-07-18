namespace Steward.Domain.Entities;

public class Part
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public required string Name { get; set; }
    public string? PartNumber { get; set; }
    public string? DefaultVendor { get; set; }
}
