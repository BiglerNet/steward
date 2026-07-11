using Steward.Application.AssetTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Steward.Api.Controllers;

[ApiController]
[Route("api/asset-types")]
public class AssetTypesController : ControllerBase
{
    /// The registry is static, non-sensitive product metadata, so it is served anonymously.
    [HttpGet]
    [AllowAnonymous]
    public ActionResult<IReadOnlyList<AssetTypeDefinition>> List()
    {
        return Ok(AssetTypeRegistry.All);
    }
}
