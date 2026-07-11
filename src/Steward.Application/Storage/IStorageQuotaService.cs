namespace Steward.Application.Storage;

public interface IStorageQuotaService
{
    /// Throws BadRequestException if adding incomingBytes would push the household's usage
    /// past its effective quota. Read-only; does not adjust the counter.
    Task EnsureCapacityAsync(Guid householdId, long incomingBytes, CancellationToken cancellationToken = default);

    /// Adjusts the household's usage counter by deltaBytes (negative to decrement) on the
    /// tracked entity. Does not call SaveChangesAsync — the caller commits it in the same
    /// transaction as its own entity write.
    Task AdjustUsageAsync(Guid householdId, long deltaBytes, CancellationToken cancellationToken = default);
}
