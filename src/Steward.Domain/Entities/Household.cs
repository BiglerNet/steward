namespace Steward.Domain.Entities;

public class Household
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string PublicSlug { get; set; }
    public bool IsPublicVisible { get; set; }
    public string? Country { get; set; }
    public string? Region { get; set; }
    public long StorageUsedBytes { get; set; }
    public long? StorageQuotaOverrideBytes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
}
