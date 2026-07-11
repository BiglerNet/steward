using Steward.Application.Regions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Steward.Api.Controllers;

[ApiController]
[Route("api/regions")]
public class RegionsController : ControllerBase
{
    /// The registry is static, non-sensitive product metadata, so it is served anonymously.
    [HttpGet]
    [AllowAnonymous]
    public ActionResult<IReadOnlyList<CountryDefinition>> List()
    {
        return Ok(RegionRegistry.All);
    }
}
