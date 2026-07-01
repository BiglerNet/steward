using System.Security.Claims;

namespace Steward.Api.Common;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException("User does not have a sub claim."));
}
