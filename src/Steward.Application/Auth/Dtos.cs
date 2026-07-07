using Steward.Domain.Enums;

namespace Steward.Application.Auth;

public record RegisterRequest(string Email, string Password, string DisplayName);

public record LoginRequest(string Email, string Password);

public record OAuthExchangeRequest(string Code);

public record PendingInviteSummary(string InviteCode, string HouseholdName, string Role, DateTimeOffset ExpiresAt);

public record AuthenticatedUser(Guid Id, string Email, string? DisplayName, ThemePreference? ThemePreference);

public record AuthResponse(
    string Token,
    DateTimeOffset ExpiresAt,
    AuthenticatedUser User,
    IReadOnlyCollection<PendingInviteSummary> PendingInvites);

public record UserProfileResponse(Guid Id, string Email, string? DisplayName, string? AvatarUrl, ThemePreference? ThemePreference);

public record UpdateThemePreferenceRequest(ThemePreference ThemePreference);

public record OAuthProvidersResponse(bool Google, bool Facebook, bool Apple);
