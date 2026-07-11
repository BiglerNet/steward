using Steward.Application.Storage;
using Steward.Domain.Common.Exceptions;
using Steward.Domain.Entities;
using Steward.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Steward.Infrastructure.Storage;

public class StorageQuotaService(StewardDbContext dbContext, IOptions<FileUploadOptions> uploadOptions)
    : IStorageQuotaService
{
    public async Task EnsureCapacityAsync(Guid householdId, long incomingBytes, CancellationToken cancellationToken = default)
    {
        var household = await FindHouseholdAsync(householdId, cancellationToken);
        var effectiveQuota = household.StorageQuotaOverrideBytes ?? uploadOptions.Value.HouseholdQuotaBytes;

        if (household.StorageUsedBytes + incomingBytes > effectiveQuota)
        {
            throw new BadRequestException("This upload would exceed the household's storage quota.");
        }
    }

    public async Task AdjustUsageAsync(Guid householdId, long deltaBytes, CancellationToken cancellationToken = default)
    {
        var household = await FindHouseholdAsync(householdId, cancellationToken);
        household.StorageUsedBytes = Math.Max(0, household.StorageUsedBytes + deltaBytes);
    }

    private async Task<Household> FindHouseholdAsync(Guid householdId, CancellationToken cancellationToken)
    {
        return await dbContext.Households.FirstOrDefaultAsync(h => h.Id == householdId, cancellationToken)
            ?? throw new NotFoundException("Household not found.");
    }
}
