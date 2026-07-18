using FluentValidation;
using Steward.Api.Common;
using Steward.Application.Assets;
using Steward.Application.Authorization;
using Steward.Application.Tracking.MaintenanceItems;
using Steward.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Steward.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/households/{householdId:guid}/assets/{assetId:guid}/maintenance-items")]
public class MaintenanceItemsController(
    IMaintenanceItemService maintenanceItemService,
    IAssetService assetService,
    IAuthorizationService authorizationService,
    IValidator<CreateMaintenanceItemRequest> createValidator,
    IValidator<PatchMaintenanceItemRequest> patchValidator,
    IValidator<CreateChecklistItemRequest> createChecklistItemValidator,
    IValidator<PatchChecklistItemRequest> patchChecklistItemValidator,
    IValidator<CreatePartLineRequest> createPartLineValidator,
    IValidator<PatchPartLineRequest> patchPartLineValidator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        Guid householdId, Guid assetId, CreateMaintenanceItemRequest request, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null) return authResult;

        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationProblem(validation.ToModelState());

        var item = await maintenanceItemService.CreateAsync(assetId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, item);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        Guid householdId, Guid assetId, [FromQuery(Name = "status")] MaintenanceItemStatus[]? status, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.View, cancellationToken);
        if (authResult is not null) return authResult;

        var items = await maintenanceItemService.ListAsync(assetId, status, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{maintenanceItemId:guid}")]
    public async Task<IActionResult> Get(
        Guid householdId, Guid assetId, Guid maintenanceItemId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.View, cancellationToken);
        if (authResult is not null) return authResult;

        var item = await maintenanceItemService.GetAsync(assetId, maintenanceItemId, cancellationToken);
        return Ok(item);
    }

    [HttpPatch("{maintenanceItemId:guid}")]
    public async Task<IActionResult> Patch(
        Guid householdId, Guid assetId, Guid maintenanceItemId, PatchMaintenanceItemRequest request, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null) return authResult;

        var validation = await patchValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationProblem(validation.ToModelState());

        var item = await maintenanceItemService.PatchAsync(assetId, maintenanceItemId, request, cancellationToken);
        return Ok(item);
    }

    [HttpDelete("{maintenanceItemId:guid}")]
    public async Task<IActionResult> Delete(
        Guid householdId, Guid assetId, Guid maintenanceItemId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null) return authResult;

        await maintenanceItemService.DeleteAsync(assetId, maintenanceItemId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{maintenanceItemId:guid}/checklist-items")]
    public async Task<IActionResult> CreateChecklistItem(
        Guid householdId, Guid assetId, Guid maintenanceItemId, CreateChecklistItemRequest request, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null) return authResult;

        var validation = await createChecklistItemValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationProblem(validation.ToModelState());

        var checklistItem = await maintenanceItemService.CreateChecklistItemAsync(assetId, maintenanceItemId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, checklistItem);
    }

    [HttpPatch("{maintenanceItemId:guid}/checklist-items/{checklistItemId:guid}")]
    public async Task<IActionResult> PatchChecklistItem(
        Guid householdId,
        Guid assetId,
        Guid maintenanceItemId,
        Guid checklistItemId,
        PatchChecklistItemRequest request,
        CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null) return authResult;

        var validation = await patchChecklistItemValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationProblem(validation.ToModelState());

        var checklistItem = await maintenanceItemService.PatchChecklistItemAsync(
            assetId, maintenanceItemId, checklistItemId, request, cancellationToken);
        return Ok(checklistItem);
    }

    [HttpDelete("{maintenanceItemId:guid}/checklist-items/{checklistItemId:guid}")]
    public async Task<IActionResult> DeleteChecklistItem(
        Guid householdId, Guid assetId, Guid maintenanceItemId, Guid checklistItemId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null) return authResult;

        await maintenanceItemService.DeleteChecklistItemAsync(assetId, maintenanceItemId, checklistItemId, cancellationToken);
        return NoContent();
    }

    [HttpPut("{maintenanceItemId:guid}/checklist-items/reorder")]
    public async Task<IActionResult> ReorderChecklistItems(
        Guid householdId, Guid assetId, Guid maintenanceItemId, ReorderChecklistItemsRequest request, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null) return authResult;

        var checklistItems = await maintenanceItemService.ReorderChecklistItemsAsync(assetId, maintenanceItemId, request, cancellationToken);
        return Ok(checklistItems);
    }

    [HttpPost("{maintenanceItemId:guid}/part-lines")]
    public async Task<IActionResult> CreatePartLine(
        Guid householdId, Guid assetId, Guid maintenanceItemId, CreatePartLineRequest request, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null) return authResult;

        var validation = await createPartLineValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationProblem(validation.ToModelState());

        var partLine = await maintenanceItemService.CreatePartLineAsync(assetId, maintenanceItemId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, partLine);
    }

    [HttpPatch("{maintenanceItemId:guid}/part-lines/{partLineId:guid}")]
    public async Task<IActionResult> PatchPartLine(
        Guid householdId,
        Guid assetId,
        Guid maintenanceItemId,
        Guid partLineId,
        PatchPartLineRequest request,
        CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null) return authResult;

        var validation = await patchPartLineValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationProblem(validation.ToModelState());

        var partLine = await maintenanceItemService.PatchPartLineAsync(assetId, maintenanceItemId, partLineId, request, cancellationToken);
        return Ok(partLine);
    }

    [HttpDelete("{maintenanceItemId:guid}/part-lines/{partLineId:guid}")]
    public async Task<IActionResult> DeletePartLine(
        Guid householdId, Guid assetId, Guid maintenanceItemId, Guid partLineId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null) return authResult;

        await maintenanceItemService.DeletePartLineAsync(assetId, maintenanceItemId, partLineId, cancellationToken);
        return NoContent();
    }

    private async Task<IActionResult?> AuthorizeAsync(
        Guid householdId,
        Guid assetId,
        OperationAuthorizationRequirement operation,
        CancellationToken cancellationToken)
    {
        var actualHouseholdId = await assetService.GetHouseholdIdForAssetAsync(assetId, cancellationToken);
        if (actualHouseholdId != householdId)
        {
            return NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(User, new HouseholdResource(householdId), operation);
        return authResult.Succeeded ? null : Forbid();
    }
}
