namespace Steward.Application.Auth;

public record RegisterRequest(string Email, string Password, string DisplayName);

public record LoginRequest(string Email, string Password);

public record OAuthExchangeRequest(string Code);

public record PendingInviteSummary(string InviteCode, string HouseholdName, string Role, DateTimeOffset ExpiresAt);

public record AuthenticatedUser(Guid Id, string Email, string? DisplayName);

public record AuthResponse(
    string Token,
    DateTimeOffset ExpiresAt,
    AuthenticatedUser User,
    IReadOnlyCollection<PendingInviteSummary> PendingInvites);

public record UserProfileResponse(Guid Id, string Email, string? DisplayName, string? AvatarUrl);
