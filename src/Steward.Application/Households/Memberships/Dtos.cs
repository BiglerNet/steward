using Steward.Domain.Enums;

namespace Steward.Application.Households.Memberships;

public record InviteMemberRequest(string Email, HouseholdMemberRole Role);

public record InvitationResponse(
    Guid Id,
    string Email,
    HouseholdMemberRole Role,
    string InviteCode,
    DateTimeOffset ExpiresAt,
    InvitationStatus Status);

public record MembershipResponse(
    Guid UserId,
    string? DisplayName,
    string Email,
    HouseholdMemberRole Role,
    HouseholdMemberStatus Status);

public record HouseholdMembersResponse(
    IReadOnlyCollection<MembershipResponse> Members,
    IReadOnlyCollection<InvitationResponse> PendingInvites);
