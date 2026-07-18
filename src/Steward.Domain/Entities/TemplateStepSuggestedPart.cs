namespace Steward.Domain.Entities;

public class TemplateStepSuggestedPart
{
    public Guid Id { get; set; }
    public Guid TemplateStepId { get; set; }
    public required string Name { get; set; }
    public decimal Quantity { get; set; } = 1;
    public int SortOrder { get; set; }
}
