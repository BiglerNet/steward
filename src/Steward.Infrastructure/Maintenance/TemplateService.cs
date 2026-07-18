using Steward.Application.Common;
using Steward.Application.Tracking.Templates;
using Steward.Domain.Common.Exceptions;
using Steward.Domain.Entities;
using Steward.Domain.Enums;
using Steward.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Steward.Infrastructure.Maintenance;

public class TemplateService(StewardDbContext dbContext) : ITemplateService
{
    public Task<TemplateResponse> CreateHouseholdTemplateAsync(
        Guid householdId, CreateTemplateRequest request, CancellationToken cancellationToken = default) =>
        CreateTemplateAsync(householdId, request, cancellationToken);

    public Task<IReadOnlyCollection<TemplateResponse>> ListHouseholdTemplatesAsync(
        Guid householdId, AssetCategory? assetCategory, CancellationToken cancellationToken = default) =>
        ListTemplatesAsync(householdId, assetCategory, cancellationToken);

    public Task<TemplateResponse> PatchHouseholdTemplateAsync(
        Guid householdId, Guid templateId, PatchTemplateRequest request, CancellationToken cancellationToken = default) =>
        PatchTemplateAsync(householdId, templateId, request, cancellationToken);

    public Task DeleteHouseholdTemplateAsync(Guid householdId, Guid templateId, CancellationToken cancellationToken = default) =>
        DeleteTemplateAsync(householdId, templateId, cancellationToken);

    public async Task<TemplateResponse> DuplicatePlatformTemplateAsync(
        Guid householdId, DuplicateTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var source = await dbContext.Templates
            .Include(t => t.Steps)
            .ThenInclude(s => s.SuggestedParts)
            .FirstOrDefaultAsync(t => t.Id == request.PlatformTemplateId, cancellationToken)
            ?? throw new NotFoundException("Template not found.");

        if (source.HouseholdId is not null)
        {
            throw new BadRequestException("platformTemplateId must reference a platform template.");
        }

        var copy = new Template
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            Title = source.Title,
            Description = source.Description,
            ApplicableCategories = [.. source.ApplicableCategories],
            Steps = source.Steps.Select(s =>
            {
                var newStepId = Guid.NewGuid();
                return new TemplateStep
                {
                    Id = newStepId,
                    Text = s.Text,
                    SortOrder = s.SortOrder,
                    EngineScoped = s.EngineScoped,
                    RecurrenceIntervalMonths = s.RecurrenceIntervalMonths,
                    RecurrenceIntervalMiles = s.RecurrenceIntervalMiles,
                    RecurrenceIntervalHours = s.RecurrenceIntervalHours,
                    SuggestedParts = s.SuggestedParts.Select(sp => new TemplateStepSuggestedPart
                    {
                        Id = Guid.NewGuid(),
                        TemplateStepId = newStepId,
                        Name = sp.Name,
                        Quantity = sp.Quantity,
                        SortOrder = sp.SortOrder,
                    }).ToList(),
                };
            }).ToList(),
        };

        dbContext.Templates.Add(copy);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(copy);
    }

    public Task<TemplateStepResponse> CreateHouseholdTemplateStepAsync(
        Guid householdId, Guid templateId, CreateTemplateStepRequest request, CancellationToken cancellationToken = default) =>
        CreateTemplateStepAsync(householdId, templateId, request, cancellationToken);

    public Task<TemplateStepResponse> PatchHouseholdTemplateStepAsync(
        Guid householdId, Guid templateId, Guid stepId, PatchTemplateStepRequest request, CancellationToken cancellationToken = default) =>
        PatchTemplateStepAsync(householdId, templateId, stepId, request, cancellationToken);

    public Task DeleteHouseholdTemplateStepAsync(
        Guid householdId, Guid templateId, Guid stepId, CancellationToken cancellationToken = default) =>
        DeleteTemplateStepAsync(householdId, templateId, stepId, cancellationToken);

    public Task<IReadOnlyList<TemplateStepResponse>> ReorderHouseholdTemplateStepsAsync(
        Guid householdId, Guid templateId, ReorderTemplateStepsRequest request, CancellationToken cancellationToken = default) =>
        ReorderTemplateStepsAsync(householdId, templateId, request, cancellationToken);

    public Task<IReadOnlyCollection<TemplateResponse>> ListPlatformTemplatesAsync(
        AssetCategory? assetCategory, CancellationToken cancellationToken = default) =>
        ListTemplatesAsync(null, assetCategory, cancellationToken);

    public Task<TemplateResponse> CreatePlatformTemplateAsync(
        CreateTemplateRequest request, CancellationToken cancellationToken = default) =>
        CreateTemplateAsync(null, request, cancellationToken);

    public Task<TemplateResponse> PatchPlatformTemplateAsync(
        Guid templateId, PatchTemplateRequest request, CancellationToken cancellationToken = default) =>
        PatchTemplateAsync(null, templateId, request, cancellationToken);

    public Task DeletePlatformTemplateAsync(Guid templateId, CancellationToken cancellationToken = default) =>
        DeleteTemplateAsync(null, templateId, cancellationToken);

    public Task<TemplateStepResponse> CreatePlatformTemplateStepAsync(
        Guid templateId, CreateTemplateStepRequest request, CancellationToken cancellationToken = default) =>
        CreateTemplateStepAsync(null, templateId, request, cancellationToken);

    public Task<TemplateStepResponse> PatchPlatformTemplateStepAsync(
        Guid templateId, Guid stepId, PatchTemplateStepRequest request, CancellationToken cancellationToken = default) =>
        PatchTemplateStepAsync(null, templateId, stepId, request, cancellationToken);

    public Task DeletePlatformTemplateStepAsync(Guid templateId, Guid stepId, CancellationToken cancellationToken = default) =>
        DeleteTemplateStepAsync(null, templateId, stepId, cancellationToken);

    public Task<IReadOnlyList<TemplateStepResponse>> ReorderPlatformTemplateStepsAsync(
        Guid templateId, ReorderTemplateStepsRequest request, CancellationToken cancellationToken = default) =>
        ReorderTemplateStepsAsync(null, templateId, request, cancellationToken);

    private async Task<TemplateResponse> CreateTemplateAsync(
        Guid? householdId, CreateTemplateRequest request, CancellationToken cancellationToken)
    {
        var template = new Template
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            Title = request.Title,
            Description = request.Description,
            ApplicableCategories = request.ApplicableCategories?.ToList() ?? [],
        };

        dbContext.Templates.Add(template);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(template);
    }

    private async Task<IReadOnlyCollection<TemplateResponse>> ListTemplatesAsync(
        Guid? householdId, AssetCategory? assetCategory, CancellationToken cancellationToken)
    {
        var query = dbContext.Templates.AsNoTracking()
            .Include(t => t.Steps)
            .ThenInclude(s => s.SuggestedParts)
            .Where(t => t.HouseholdId == householdId);

        if (assetCategory is { } category)
        {
            query = query.Where(t => t.ApplicableCategories.Count == 0 || t.ApplicableCategories.Contains(category));
        }

        var templates = await query.ToListAsync(cancellationToken);
        return templates.Select(ToResponse).ToList();
    }

    private async Task<TemplateResponse> PatchTemplateAsync(
        Guid? householdId, Guid templateId, PatchTemplateRequest request, CancellationToken cancellationToken)
    {
        var template = await FindTemplateAsync(householdId, templateId, cancellationToken);

        request.Title.IfSet(v => template.Title = v!);
        request.Description.IfSet(v => template.Description = v);
        request.ApplicableCategories.IfSet(v => template.ApplicableCategories = v ?? []);

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(template);
    }

    private async Task DeleteTemplateAsync(Guid? householdId, Guid templateId, CancellationToken cancellationToken)
    {
        var template = await FindTemplateAsync(householdId, templateId, cancellationToken, includeSteps: false);
        dbContext.Templates.Remove(template);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<TemplateStepResponse> CreateTemplateStepAsync(
        Guid? householdId, Guid templateId, CreateTemplateStepRequest request, CancellationToken cancellationToken)
    {
        await FindTemplateAsync(householdId, templateId, cancellationToken, includeSteps: false);

        var nextSortOrder = await dbContext.TemplateSteps.AsNoTracking()
            .Where(s => s.TemplateId == templateId)
            .Select(s => (int?)s.SortOrder)
            .MaxAsync(cancellationToken) is { } max ? max + 1 : 0;

        var step = new TemplateStep
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            Text = request.Text,
            SortOrder = nextSortOrder,
            EngineScoped = request.EngineScoped ?? false,
            RecurrenceIntervalMonths = request.RecurrenceIntervalMonths,
            RecurrenceIntervalMiles = request.RecurrenceIntervalMiles,
            RecurrenceIntervalHours = request.RecurrenceIntervalHours,
            SuggestedParts = ToSuggestedParts(request.SuggestedParts),
        };

        dbContext.TemplateSteps.Add(step);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToStepResponse(step);
    }

    private async Task<TemplateStepResponse> PatchTemplateStepAsync(
        Guid? householdId, Guid templateId, Guid stepId, PatchTemplateStepRequest request, CancellationToken cancellationToken)
    {
        await FindTemplateAsync(householdId, templateId, cancellationToken, includeSteps: false);
        var step = await FindTemplateStepAsync(templateId, stepId, cancellationToken);

        request.Text.IfSet(v => step.Text = v!);
        request.EngineScoped.IfSet(v => step.EngineScoped = v);
        request.RecurrenceIntervalMonths.IfSet(v => step.RecurrenceIntervalMonths = v);
        request.RecurrenceIntervalMiles.IfSet(v => step.RecurrenceIntervalMiles = v);
        request.RecurrenceIntervalHours.IfSet(v => step.RecurrenceIntervalHours = v);
        request.SuggestedParts.IfSet(v => step.SuggestedParts = ToSuggestedParts(v));

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToStepResponse(step);
    }

    private async Task DeleteTemplateStepAsync(
        Guid? householdId, Guid templateId, Guid stepId, CancellationToken cancellationToken)
    {
        await FindTemplateAsync(householdId, templateId, cancellationToken, includeSteps: false);
        var step = await FindTemplateStepAsync(templateId, stepId, cancellationToken);
        dbContext.TemplateSteps.Remove(step);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<TemplateStepResponse>> ReorderTemplateStepsAsync(
        Guid? householdId, Guid templateId, ReorderTemplateStepsRequest request, CancellationToken cancellationToken)
    {
        await FindTemplateAsync(householdId, templateId, cancellationToken, includeSteps: false);

        var steps = await dbContext.TemplateSteps
            .Where(s => s.TemplateId == templateId)
            .ToListAsync(cancellationToken);

        var existingIds = steps.Select(s => s.Id).ToHashSet();
        if (request.StepIds.Count != existingIds.Count || !request.StepIds.ToHashSet().SetEquals(existingIds))
        {
            throw new BadRequestException("Reorder request must include exactly the template's existing step ids.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var byId = steps.ToDictionary(s => s.Id);
        for (var index = 0; index < request.StepIds.Count; index++)
        {
            byId[request.StepIds[index]].SortOrder = index;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return steps.OrderBy(s => s.SortOrder).Select(ToStepResponse).ToList();
    }

    private async Task<Template> FindTemplateAsync(
        Guid? householdId, Guid templateId, CancellationToken cancellationToken, bool includeSteps = true)
    {
        var query = dbContext.Templates.AsQueryable();
        if (includeSteps)
        {
            query = query.Include(t => t.Steps.OrderBy(s => s.SortOrder))
                .ThenInclude(s => s.SuggestedParts);
        }

        return await query.FirstOrDefaultAsync(t => t.Id == templateId && t.HouseholdId == householdId, cancellationToken)
            ?? throw new NotFoundException("Template not found.");
    }

    private async Task<TemplateStep> FindTemplateStepAsync(Guid templateId, Guid stepId, CancellationToken cancellationToken)
    {
        return await dbContext.TemplateSteps
            .Include(s => s.SuggestedParts)
            .FirstOrDefaultAsync(s => s.Id == stepId && s.TemplateId == templateId, cancellationToken)
            ?? throw new NotFoundException("Template step not found.");
    }

    private static List<TemplateStepSuggestedPart> ToSuggestedParts(IReadOnlyList<SuggestedPartDto>? dtos) =>
        dtos?.Select((d, index) => new TemplateStepSuggestedPart
        {
            Id = Guid.NewGuid(),
            Name = d.Name,
            Quantity = d.Quantity,
            SortOrder = index,
        }).ToList() ?? [];

    private static TemplateResponse ToResponse(Template template) => new(
        template.Id,
        template.HouseholdId,
        template.Title,
        template.Description,
        template.ApplicableCategories,
        template.Steps.OrderBy(s => s.SortOrder).Select(ToStepResponse).ToList());

    private static TemplateStepResponse ToStepResponse(TemplateStep step) => new(
        step.Id,
        step.TemplateId,
        step.Text,
        step.SortOrder,
        step.EngineScoped,
        step.RecurrenceIntervalMonths,
        step.RecurrenceIntervalMiles,
        step.RecurrenceIntervalHours,
        step.SuggestedParts.OrderBy(sp => sp.SortOrder).Select(sp => new SuggestedPartDto(sp.Name, sp.Quantity)).ToList());
}
