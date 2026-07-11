namespace Steward.Application.Identity;

public interface IRefreshTokenService
{
    Task<RefreshTokenIssueResult> IssueAsync(Guid userId, bool rememberMe, CancellationToken cancellationToken = default);

    Task<RefreshTokenRotateResult?> RotateAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task RevokeChainAsync(string refreshToken, CancellationToken cancellationToken = default);
}

public record RefreshTokenIssueResult(string Token, DateTimeOffset ExpiresAt);

public record RefreshTokenRotateResult(Guid UserId, string Token, DateTimeOffset ExpiresAt);
