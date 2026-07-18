using FluentValidation;
using Steward.Api.Common;
using Steward.Application.Tracking.Templates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Steward.Api.Controllers;

[Authorize(Roles = "PlatformAdmin")]
[ApiController]
[Route("api/admin/templates")]
public class AdminTemplatesController(
    ITemplateService templateService,
    IValidator<CreateTemplateRequest> createValidator,
    IValidator<PatchTemplateRequest> patchValidator,
    IValidator<CreateTemplateStepRequest> createStepValidator,
    IValidator<PatchTemplateStepRequest> patchStepValidator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateTemplateRequest request, CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationProblem(validation.ToModelState());

        var template = await templateService.CreatePlatformTemplateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, template);
    }

    [HttpPatch("{templateId:guid}")]
    public async Task<IActionResult> Patch(Guid templateId, PatchTemplateRequest request, CancellationToken cancellationToken)
    {
        var validation = await patchValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationProblem(validation.ToModelState());

        var template = await templateService.PatchPlatformTemplateAsync(templateId, request, cancellationToken);
        return Ok(template);
    }

    [HttpDelete("{templateId:guid}")]
    public async Task<IActionResult> Delete(Guid templateId, CancellationToken cancellationToken)
    {
        await templateService.DeletePlatformTemplateAsync(templateId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{templateId:guid}/steps")]
    public async Task<IActionResult> CreateStep(
        Guid templateId, CreateTemplateStepRequest request, CancellationToken cancellationToken)
    {
        var validation = await createStepValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationProblem(validation.ToModelState());

        var step = await templateService.CreatePlatformTemplateStepAsync(templateId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, step);
    }

    [HttpPatch("{templateId:guid}/steps/{stepId:guid}")]
    public async Task<IActionResult> PatchStep(
        Guid templateId, Guid stepId, PatchTemplateStepRequest request, CancellationToken cancellationToken)
    {
        var validation = await patchStepValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationProblem(validation.ToModelState());

        var step = await templateService.PatchPlatformTemplateStepAsync(templateId, stepId, request, cancellationToken);
        return Ok(step);
    }

    [HttpDelete("{templateId:guid}/steps/{stepId:guid}")]
    public async Task<IActionResult> DeleteStep(Guid templateId, Guid stepId, CancellationToken cancellationToken)
    {
        await templateService.DeletePlatformTemplateStepAsync(templateId, stepId, cancellationToken);
        return NoContent();
    }

    [HttpPut("{templateId:guid}/steps/reorder")]
    public async Task<IActionResult> ReorderSteps(
        Guid templateId, ReorderTemplateStepsRequest request, CancellationToken cancellationToken)
    {
        var steps = await templateService.ReorderPlatformTemplateStepsAsync(templateId, request, cancellationToken);
        return Ok(steps);
    }
}
