using Steward.Application.Common;
using Steward.Domain.Enums;

namespace Steward.Application.Tracking.Templates;

public record SuggestedPartDto(string Name, decimal Quantity);

public record TemplateResponse(
    Guid Id,
    Guid? HouseholdId,
    string Title,
    string? Description,
    IReadOnlyList<AssetCategory> ApplicableCategories,
    IReadOnlyList<TemplateStepResponse> Steps);

public record TemplateStepResponse(
    Guid Id,
    Guid TemplateId,
    string Text,
    int SortOrder,
    bool EngineScoped,
    int? RecurrenceIntervalMonths,
    decimal? RecurrenceIntervalMiles,
    decimal? RecurrenceIntervalHours,
    IReadOnlyList<SuggestedPartDto> SuggestedParts);

public record CreateTemplateRequest(
    string Title,
    string? Description,
    IReadOnlyList<AssetCategory>? ApplicableCategories);

public record PatchTemplateRequest(
    Optional<string> Title,
    Optional<string?> Description,
    Optional<List<AssetCategory>?> ApplicableCategories);

public record CreateTemplateStepRequest(
    string Text,
    bool? EngineScoped,
    int? RecurrenceIntervalMonths,
    decimal? RecurrenceIntervalMiles,
    decimal? RecurrenceIntervalHours,
    IReadOnlyList<SuggestedPartDto>? SuggestedParts);

public record PatchTemplateStepRequest(
    Optional<string> Text,
    Optional<bool> EngineScoped,
    Optional<int?> RecurrenceIntervalMonths,
    Optional<decimal?> RecurrenceIntervalMiles,
    Optional<decimal?> RecurrenceIntervalHours,
    Optional<List<SuggestedPartDto>?> SuggestedParts);

public record ReorderTemplateStepsRequest(IReadOnlyList<Guid> StepIds);

public record DuplicateTemplateRequest(Guid PlatformTemplateId);
