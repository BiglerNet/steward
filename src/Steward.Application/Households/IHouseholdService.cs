using Steward.Application.Households.Memberships;

namespace Steward.Application.Households;

public interface IHouseholdService
{
    Task<HouseholdResponse> CreateAsync(Guid userId, CreateHouseholdRequest request, CancellationToken cancellationToken = default);

    Task<HouseholdResponse> GetByIdAsync(Guid userId, Guid householdId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<HouseholdResponse>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<HouseholdResponse> UpdateAsync(Guid householdId, UpdateHouseholdRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid householdId, CancellationToken cancellationToken = default);

    Task<InvitationResponse> InviteMemberAsync(Guid householdId, Guid invitedByUserId, InviteMemberRequest request, CancellationToken cancellationToken = default);

    Task AcceptInviteAsync(Guid userId, string inviteCode, CancellationToken cancellationToken = default);

    Task RevokeInviteAsync(Guid householdId, string inviteCode, CancellationToken cancellationToken = default);

    Task RevokeMemberAsync(Guid householdId, Guid actingUserId, Guid targetUserId, CancellationToken cancellationToken = default);

    Task<HouseholdMembersResponse> ListMembersAsync(Guid householdId, CancellationToken cancellationToken = default);

    Task SetStorageQuotaOverrideAsync(Guid householdId, long? quotaBytes, CancellationToken cancellationToken = default);
}
