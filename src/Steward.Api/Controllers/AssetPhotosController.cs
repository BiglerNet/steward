using FluentValidation;
using Steward.Api.Common;
using Steward.Application.Assets;
using Steward.Application.Authorization;
using Steward.Application.Photos;
using Steward.Application.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Steward.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/households/{householdId:guid}/assets/{assetId:guid}/photos")]
public class AssetPhotosController(
    IAssetPhotoService assetPhotoService,
    IAssetService assetService,
    IAuthorizationService authorizationService,
    IOptions<FileUploadOptions> uploadOptions,
    IValidator<SetCoverPhotoRequest> setCoverValidator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Upload(
        Guid householdId, Guid assetId, IFormFile file, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        if (file.Length > uploadOptions.Value.MaxPhotoUploadSizeBytes)
        {
            return BadRequest("File exceeds the maximum allowed size.");
        }

        await using var stream = file.OpenReadStream();
        var photo = await assetPhotoService.UploadAsync(householdId, assetId, stream, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, photo);
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid householdId, Guid assetId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.View, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var photos = await assetPhotoService.ListAsync(assetId, cancellationToken);
        return Ok(photos);
    }

    [HttpGet("{photoId:guid}/content")]
    public async Task<IActionResult> GetContent(
        Guid householdId, Guid assetId, Guid photoId, [FromQuery] string? variant, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.View, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var (content, contentType) = await assetPhotoService.OpenVariantAsync(assetId, photoId, variant, cancellationToken);
        return File(content, contentType);
    }

    [HttpDelete("{photoId:guid}")]
    public async Task<IActionResult> Delete(
        Guid householdId, Guid assetId, Guid photoId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        await assetPhotoService.DeleteAsync(householdId, assetId, photoId, cancellationToken);
        return NoContent();
    }

    [HttpPut("/api/households/{householdId:guid}/assets/{assetId:guid}/cover-photo")]
    public async Task<IActionResult> SetCover(
        Guid householdId, Guid assetId, SetCoverPhotoRequest request, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var validation = await setCoverValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelState());
        }

        await assetPhotoService.SetCoverAsync(assetId, request.PhotoId, cancellationToken);

        var asset = await assetService.GetByIdAsync(householdId, assetId, cancellationToken);
        return Ok(asset);
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
