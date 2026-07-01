using Steward.Domain.Enums;

namespace Steward.Domain.Entities;

public class HouseholdInvitation
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public Guid InvitedByUserId { get; set; }
    public required string Email { get; set; }
    public HouseholdMemberRole Role { get; set; }
    public required string InviteCode { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public InvitationStatus Status { get; set; }
    public Guid? AcceptedByUserId { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
