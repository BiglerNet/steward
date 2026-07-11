using Steward.Application.VinDecode;
using Steward.Domain.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Steward.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/vin-decode")]
public class VinDecodeController(IVinDecodeService vinDecodeService) : ControllerBase
{
    [HttpGet("{vin}")]
    public async Task<ActionResult<VinDecodeResult>> Decode(string vin, CancellationToken cancellationToken)
    {
        if (!VinFormat.IsValid(vin))
        {
            throw new BadRequestException("VIN must be exactly 17 alphanumeric characters excluding I, O, and Q.");
        }

        var result = await vinDecodeService.DecodeAsync(vin, cancellationToken);
        return Ok(result);
    }
}
