namespace Steward.Domain.Entities;

public class Household
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string PublicSlug { get; set; }
    public bool IsPublicVisible { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
}
