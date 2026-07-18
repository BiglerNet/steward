namespace Steward.Domain.Entities;

public class TemplateStep
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public required string Text { get; set; }
    public int SortOrder { get; set; }
    public bool EngineScoped { get; set; }
    public int? RecurrenceIntervalMonths { get; set; }
    public decimal? RecurrenceIntervalMiles { get; set; }
    public decimal? RecurrenceIntervalHours { get; set; }
    public List<TemplateStepSuggestedPart> SuggestedParts { get; set; } = [];
}
