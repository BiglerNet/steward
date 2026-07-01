namespace Steward.Domain.Entities;

public class Warranty
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public required string Provider { get; set; }
    public string? Description { get; set; }
    public DateOnly? StartsOn { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public string? DocumentUrl { get; set; }
    public string? Notes { get; set; }
}
