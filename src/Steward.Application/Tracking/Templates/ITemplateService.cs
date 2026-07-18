using Steward.Domain.Enums;

namespace Steward.Application.Tracking.Templates;

public interface ITemplateService
{
    Task<TemplateResponse> CreateHouseholdTemplateAsync(
        Guid householdId, CreateTemplateRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TemplateResponse>> ListHouseholdTemplatesAsync(
        Guid householdId, AssetCategory? assetCategory, CancellationToken cancellationToken = default);

    Task<TemplateResponse> PatchHouseholdTemplateAsync(
        Guid householdId, Guid templateId, PatchTemplateRequest request, CancellationToken cancellationToken = default);

    Task DeleteHouseholdTemplateAsync(Guid householdId, Guid templateId, CancellationToken cancellationToken = default);

    Task<TemplateResponse> DuplicatePlatformTemplateAsync(
        Guid householdId, DuplicateTemplateRequest request, CancellationToken cancellationToken = default);

    Task<TemplateStepResponse> CreateHouseholdTemplateStepAsync(
        Guid householdId, Guid templateId, CreateTemplateStepRequest request, CancellationToken cancellationToken = default);

    Task<TemplateStepResponse> PatchHouseholdTemplateStepAsync(
        Guid householdId, Guid templateId, Guid stepId, PatchTemplateStepRequest request, CancellationToken cancellationToken = default);

    Task DeleteHouseholdTemplateStepAsync(
        Guid householdId, Guid templateId, Guid stepId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TemplateStepResponse>> ReorderHouseholdTemplateStepsAsync(
        Guid householdId, Guid templateId, ReorderTemplateStepsRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TemplateResponse>> ListPlatformTemplatesAsync(
        AssetCategory? assetCategory, CancellationToken cancellationToken = default);

    Task<TemplateResponse> CreatePlatformTemplateAsync(
        CreateTemplateRequest request, CancellationToken cancellationToken = default);

    Task<TemplateResponse> PatchPlatformTemplateAsync(
        Guid templateId, PatchTemplateRequest request, CancellationToken cancellationToken = default);

    Task DeletePlatformTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);

    Task<TemplateStepResponse> CreatePlatformTemplateStepAsync(
        Guid templateId, CreateTemplateStepRequest request, CancellationToken cancellationToken = default);

    Task<TemplateStepResponse> PatchPlatformTemplateStepAsync(
        Guid templateId, Guid stepId, PatchTemplateStepRequest request, CancellationToken cancellationToken = default);

    Task DeletePlatformTemplateStepAsync(Guid templateId, Guid stepId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TemplateStepResponse>> ReorderPlatformTemplateStepsAsync(
        Guid templateId, ReorderTemplateStepsRequest request, CancellationToken cancellationToken = default);
}
