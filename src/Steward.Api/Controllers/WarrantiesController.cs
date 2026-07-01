using FluentValidation;
using Steward.Api.Common;
using Steward.Application.Assets;
using Steward.Application.Authorization;
using Steward.Application.Storage;
using Steward.Application.Tracking.Warranties;
using Steward.Domain.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Steward.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/households/{householdId:guid}/assets/{assetId:guid}/warranties")]
public class WarrantiesController(
    IWarrantyService warrantyService,
    IAssetService assetService,
    IAuthorizationService authorizationService,
    IFileStorageService fileStorageService,
    IOptions<FileUploadOptions> uploadOptions,
    IValidator<CreateWarrantyRequest> createValidator,
    IValidator<UpdateWarrantyRequest> updateValidator) : ControllerBase
{
    private const string EntityType = "warranties";

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid householdId, Guid assetId, CreateWarrantyRequest request, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelState());
        }

        var warranty = await warrantyService.CreateAsync(assetId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, WithDocumentUrl(householdId, assetId, warranty));
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid householdId, Guid assetId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.View, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var warranties = await warrantyService.ListAsync(assetId, cancellationToken);
        return Ok(warranties.Select(w => WithDocumentUrl(householdId, assetId, w)));
    }

    [HttpPut("{warrantyId:guid}")]
    public async Task<IActionResult> Update(
        Guid householdId,
        Guid assetId,
        Guid warrantyId,
        UpdateWarrantyRequest request,
        CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelState());
        }

        var warranty = await warrantyService.UpdateAsync(assetId, warrantyId, request, cancellationToken);
        return Ok(WithDocumentUrl(householdId, assetId, warranty));
    }

    [HttpDelete("{warrantyId:guid}")]
    public async Task<IActionResult> Delete(
        Guid householdId, Guid assetId, Guid warrantyId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        await warrantyService.DeleteAsync(assetId, warrantyId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{warrantyId:guid}/document")]
    public async Task<IActionResult> UploadDocument(
        Guid householdId, Guid assetId, Guid warrantyId, IFormFile file, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var options = uploadOptions.Value;
        if (!options.AllowedContentTypes.Contains(file.ContentType))
        {
            return BadRequest("Unsupported content type.");
        }

        if (file.Length > options.MaxUploadSizeBytes)
        {
            return BadRequest("File exceeds the maximum allowed size.");
        }

        await using var stream = file.OpenReadStream();
        var storageKey = await fileStorageService.SaveAsync(
            stream, file.ContentType, EntityType, warrantyId, cancellationToken);

        var warranty = await warrantyService.SetDocumentAsync(assetId, warrantyId, storageKey, cancellationToken);

        return Ok(WithDocumentUrl(householdId, assetId, warranty));
    }

    [HttpGet("{warrantyId:guid}/document")]
    public async Task<IActionResult> DownloadDocument(
        Guid householdId, Guid assetId, Guid warrantyId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.View, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var storageKey = await warrantyService.GetDocumentStorageKeyAsync(assetId, warrantyId, cancellationToken)
            ?? throw new NotFoundException("Document not found.");

        var (content, contentType) = await fileStorageService.OpenReadAsync(storageKey, cancellationToken);
        return File(content, contentType);
    }

    [HttpDelete("{warrantyId:guid}/document")]
    public async Task<IActionResult> DeleteDocument(
        Guid householdId, Guid assetId, Guid warrantyId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        await warrantyService.RemoveDocumentAsync(assetId, warrantyId, cancellationToken);
        return NoContent();
    }

    private WarrantyResponse WithDocumentUrl(Guid householdId, Guid assetId, WarrantyResponse warranty) =>
        warranty.HasDocument
            ? warranty with
            {
                DocumentUrl = Url.Action(
                    nameof(DownloadDocument),
                    values: new { householdId, assetId, warrantyId = warranty.Id }),
            }
            : warranty;

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
