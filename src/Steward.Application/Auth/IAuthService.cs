namespace Steward.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<string> HandleOAuthCallbackAsync(
        string provider, string providerKey, string email, string? displayName, CancellationToken cancellationToken = default);

    Task<AuthResponse> ExchangeOAuthCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default);

    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);
}
