using FluentValidation;
using Steward.Api.Common;
using Steward.Application.Authorization;
using Steward.Application.Tracking.Templates;
using Steward.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Steward.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/households/{householdId:guid}/templates")]
public class TemplatesController(
    ITemplateService templateService,
    IAuthorizationService authorizationService,
    IValidator<CreateTemplateRequest> createValidator,
    IValidator<PatchTemplateRequest> patchValidator,
    IValidator<CreateTemplateStepRequest> createStepValidator,
    IValidator<PatchTemplateStepRequest> patchStepValidator,
    IValidator<DuplicateTemplateRequest> duplicateValidator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(Guid householdId, CreateTemplateRequest request, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, HouseholdOperations.Edit);
        if (!authResult.Succeeded) return Forbid();

        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationProblem(validation.ToModelState());

        var template = await templateService.CreateHouseholdTemplateAsync(householdId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, template);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        Guid householdId, [FromQuery] AssetCategory? assetCategory, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, HouseholdOperations.View);
        if (!authResult.Succeeded) return Forbid();

        var templates = await templateService.ListHouseholdTemplatesAsync(householdId, assetCategory, cancellationToken);
        return Ok(templates);
    }

    [HttpPatch("{templateId:guid}")]
    public async Task<IActionResult> Patch(
        Guid householdId, Guid templateId, PatchTemplateRequest request, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, HouseholdOperations.Edit);
        if (!authResult.Succeeded) return Forbid();

        var validation = await patchValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationProblem(validation.ToModelState());

        var template = await templateService.PatchHouseholdTemplateAsync(householdId, templateId, request, cancellationToken);
        return Ok(template);
    }

    [HttpDelete("{templateId:guid}")]
    public async Task<IActionResult> Delete(Guid householdId, Guid templateId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, HouseholdOperations.Edit);
        if (!authResult.Succeeded) return Forbid();

        await templateService.DeleteHouseholdTemplateAsync(householdId, templateId, cancellationToken);
        return NoContent();
    }

    [HttpPost("duplicate")]
    public async Task<IActionResult> Duplicate(
        Guid householdId, DuplicateTemplateRequest request, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, HouseholdOperations.Edit);
        if (!authResult.Succeeded) return Forbid();

        var validation = await duplicateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationProblem(validation.ToModelState());

        var template = await templateService.DuplicatePlatformTemplateAsync(householdId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, template);
    }

    [HttpPost("{templateId:guid}/steps")]
    public async Task<IActionResult> CreateStep(
        Guid householdId, Guid templateId, CreateTemplateStepRequest request, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, HouseholdOperations.Edit);
        if (!authResult.Succeeded) return Forbid();

        var validation = await createStepValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationProblem(validation.ToModelState());

        var step = await templateService.CreateHouseholdTemplateStepAsync(householdId, templateId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, step);
    }

    [HttpPatch("{templateId:guid}/steps/{stepId:guid}")]
    public async Task<IActionResult> PatchStep(
        Guid householdId, Guid templateId, Guid stepId, PatchTemplateStepRequest request, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, HouseholdOperations.Edit);
        if (!authResult.Succeeded) return Forbid();

        var validation = await patchStepValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationProblem(validation.ToModelState());

        var step = await templateService.PatchHouseholdTemplateStepAsync(householdId, templateId, stepId, request, cancellationToken);
        return Ok(step);
    }

    [HttpDelete("{templateId:guid}/steps/{stepId:guid}")]
    public async Task<IActionResult> DeleteStep(
        Guid householdId, Guid templateId, Guid stepId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, HouseholdOperations.Edit);
        if (!authResult.Succeeded) return Forbid();

        await templateService.DeleteHouseholdTemplateStepAsync(householdId, templateId, stepId, cancellationToken);
        return NoContent();
    }

    [HttpPut("{templateId:guid}/steps/reorder")]
    public async Task<IActionResult> ReorderSteps(
        Guid householdId, Guid templateId, ReorderTemplateStepsRequest request, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, HouseholdOperations.Edit);
        if (!authResult.Succeeded) return Forbid();

        var steps = await templateService.ReorderHouseholdTemplateStepsAsync(householdId, templateId, request, cancellationToken);
        return Ok(steps);
    }

    private Task<AuthorizationResult> AuthorizeAsync(
        Guid householdId, Microsoft.AspNetCore.Authorization.Infrastructure.OperationAuthorizationRequirement operation) =>
        authorizationService.AuthorizeAsync(User, new HouseholdResource(householdId), operation);
}
