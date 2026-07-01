namespace Steward.Application.Households;

public interface IInvitationExpiryService
{
    Task ExpireStaleInvitationsAsync(CancellationToken cancellationToken = default);
}
