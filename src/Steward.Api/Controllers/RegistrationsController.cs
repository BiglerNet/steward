using FluentValidation;
using Steward.Api.Common;
using Steward.Application.Assets;
using Steward.Application.Authorization;
using Steward.Application.Storage;
using Steward.Application.Tracking.Registrations;
using Steward.Domain.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Steward.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/households/{householdId:guid}/assets/{assetId:guid}/registrations")]
public class RegistrationsController(
    IRegistrationService registrationService,
    IAssetService assetService,
    IAuthorizationService authorizationService,
    IFileStorageService fileStorageService,
    IStorageQuotaService storageQuotaService,
    IOptions<FileUploadOptions> uploadOptions,
    IValidator<CreateRegistrationRequest> createValidator,
    IValidator<UpdateRegistrationRequest> updateValidator) : ControllerBase
{
    private const string EntityType = "registrations";

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid householdId, Guid assetId, CreateRegistrationRequest request, CancellationToken cancellationToken)
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

        var registration = await registrationService.CreateAsync(assetId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, WithDocumentUrl(householdId, assetId, registration));
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid householdId, Guid assetId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.View, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var registrations = await registrationService.ListAsync(assetId, cancellationToken);
        return Ok(registrations.Select(r => WithDocumentUrl(householdId, assetId, r)));
    }

    [HttpPut("{registrationId:guid}")]
    public async Task<IActionResult> Update(
        Guid householdId,
        Guid assetId,
        Guid registrationId,
        UpdateRegistrationRequest request,
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

        var registration = await registrationService.UpdateAsync(assetId, registrationId, request, cancellationToken);
        return Ok(WithDocumentUrl(householdId, assetId, registration));
    }

    [HttpDelete("{registrationId:guid}")]
    public async Task<IActionResult> Delete(
        Guid householdId, Guid assetId, Guid registrationId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        await registrationService.DeleteAsync(householdId, assetId, registrationId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{registrationId:guid}/document")]
    public async Task<IActionResult> UploadDocument(
        Guid householdId, Guid assetId, Guid registrationId, IFormFile file, CancellationToken cancellationToken)
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

        await storageQuotaService.EnsureCapacityAsync(householdId, file.Length, cancellationToken);

        await using var stream = file.OpenReadStream();
        var storageKey = await fileStorageService.SaveAsync(
            stream, file.ContentType, EntityType, registrationId, cancellationToken);

        var registration = await registrationService.SetDocumentAsync(
            householdId, assetId, registrationId, storageKey, file.Length, cancellationToken);

        return Ok(WithDocumentUrl(householdId, assetId, registration));
    }

    [HttpGet("{registrationId:guid}/document")]
    public async Task<IActionResult> DownloadDocument(
        Guid householdId, Guid assetId, Guid registrationId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.View, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var storageKey = await registrationService.GetDocumentStorageKeyAsync(assetId, registrationId, cancellationToken)
            ?? throw new NotFoundException("Document not found.");

        var (content, contentType) = await fileStorageService.OpenReadAsync(storageKey, cancellationToken);
        return File(content, contentType);
    }

    [HttpDelete("{registrationId:guid}/document")]
    public async Task<IActionResult> DeleteDocument(
        Guid householdId, Guid assetId, Guid registrationId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        await registrationService.RemoveDocumentAsync(householdId, assetId, registrationId, cancellationToken);
        return NoContent();
    }

    private RegistrationResponse WithDocumentUrl(Guid householdId, Guid assetId, RegistrationResponse registration) =>
        registration.HasDocument
            ? registration with
            {
                DocumentUrl = Url.Action(
                    nameof(DownloadDocument),
                    values: new { householdId, assetId, registrationId = registration.Id }),
            }
            : registration;

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
