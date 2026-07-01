using Steward.Domain.Enums;

namespace Steward.Domain.Entities;

public class HouseholdMembership
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public Guid UserId { get; set; }
    public HouseholdMemberRole Role { get; set; }
    public HouseholdMemberStatus Status { get; set; }
    public Guid? InvitedByUserId { get; set; }
    public DateTimeOffset InvitedAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
}
