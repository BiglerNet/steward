using Steward.Application.Tracking.Templates;
using Steward.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Steward.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/templates")]
public class PlatformTemplateCatalogController(ITemplateService templateService) : ControllerBase
{
    [HttpGet("platform")]
    public async Task<IActionResult> ListPlatformTemplates(
        [FromQuery] AssetCategory? assetCategory, CancellationToken cancellationToken)
    {
        var templates = await templateService.ListPlatformTemplatesAsync(assetCategory, cancellationToken);
        return Ok(templates);
    }
}
