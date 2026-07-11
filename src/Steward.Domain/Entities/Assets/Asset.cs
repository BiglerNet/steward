using Steward.Domain.Enums;

namespace Steward.Domain.Entities.Assets;

public abstract class Asset
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public AssetCategory Category { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int? Year { get; set; }
    public Guid? CoverPhotoId { get; set; }
    public UsageTrackingMode UsageTrackingMode { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
