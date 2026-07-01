namespace Steward.Application.Identity;

public interface IJwtTokenService
{
    string GenerateToken(JwtTokenRequest request);
}

public record JwtTokenRequest(Guid UserId, string Email, string? DisplayName, IReadOnlyCollection<string> Roles);
