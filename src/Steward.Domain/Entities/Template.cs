using Steward.Domain.Enums;

namespace Steward.Domain.Entities;

public class Template
{
    public Guid Id { get; set; }
    public Guid? HouseholdId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public List<AssetCategory> ApplicableCategories { get; set; } = [];

    public List<TemplateStep> Steps { get; set; } = [];
}
