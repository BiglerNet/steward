using FluentValidation;
using Steward.Api.Common;
using Steward.Application.Assets;
using Steward.Application.Authorization;
using Steward.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Steward.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/households/{householdId:guid}/assets")]
public class AssetsController(
    IAssetService assetService,
    IAuthorizationService authorizationService,
    IValidator<CreateAssetRequest> createValidator,
    IValidator<UpdateAssetRequest> updateValidator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        Guid householdId, CreateAssetRequest request, CancellationToken cancellationToken)
    {
        var authResult = await authorizationService.AuthorizeAsync(
            User, new HouseholdResource(householdId), HouseholdOperations.Edit);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelState());
        }

        var asset = await assetService.CreateAsync(householdId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { householdId, assetId = asset.Id }, asset);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        Guid householdId,
        [FromQuery] AssetCategory? category,
        [FromQuery] AssetGroup? group,
        CancellationToken cancellationToken)
    {
        var authResult = await authorizationService.AuthorizeAsync(
            User, new HouseholdResource(householdId), HouseholdOperations.View);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        var assets = await assetService.ListAsync(householdId, category, group, cancellationToken);
        return Ok(assets);
    }

    [HttpGet("{assetId:guid}")]
    public async Task<IActionResult> GetById(Guid householdId, Guid assetId, CancellationToken cancellationToken)
    {
        var authResult = await authorizationService.AuthorizeAsync(
            User, new HouseholdResource(householdId), HouseholdOperations.View);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        var asset = await assetService.GetByIdAsync(householdId, assetId, cancellationToken);
        return Ok(asset);
    }

    [HttpPut("{assetId:guid}")]
    public async Task<IActionResult> Update(
        Guid householdId, Guid assetId, UpdateAssetRequest request, CancellationToken cancellationToken)
    {
        var authResult = await authorizationService.AuthorizeAsync(
            User, new HouseholdResource(householdId), HouseholdOperations.Edit);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelState());
        }

        var asset = await assetService.UpdateAsync(householdId, assetId, request, cancellationToken);
        return Ok(asset);
    }

    [HttpDelete("{assetId:guid}")]
    public async Task<IActionResult> Delete(Guid householdId, Guid assetId, CancellationToken cancellationToken)
    {
        var authResult = await authorizationService.AuthorizeAsync(
            User, new HouseholdResource(householdId), HouseholdOperations.Delete);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        await assetService.DeleteAsync(householdId, assetId, cancellationToken);
        return NoContent();
    }
}
