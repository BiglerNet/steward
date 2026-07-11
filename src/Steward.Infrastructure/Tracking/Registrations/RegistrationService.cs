using Steward.Application.Storage;
using Steward.Application.Tracking.Registrations;
using Steward.Domain.Common.Exceptions;
using Steward.Domain.Entities;
using Steward.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Steward.Infrastructure.Tracking.Registrations;

public class RegistrationService(
    StewardDbContext dbContext, IFileStorageService fileStorageService, IStorageQuotaService storageQuotaService)
    : IRegistrationService
{
    public async Task<RegistrationResponse> CreateAsync(
        Guid assetId, CreateRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        var registration = new Registration
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            Kind = request.Kind!.Value,
            RegistrationNumber = request.RegistrationNumber,
            IssuingAuthority = request.IssuingAuthority,
            ValidFrom = request.ValidFrom,
            RenewedOn = request.RenewedOn,
            Cost = request.Cost,
            ExpiresOn = request.ExpiresOn,
            Notes = request.Notes,
        };

        dbContext.Registrations.Add(registration);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(registration);
    }

    public async Task<IReadOnlyCollection<RegistrationResponse>> ListAsync(
        Guid assetId, CancellationToken cancellationToken = default)
    {
        var registrations = await dbContext.Registrations.AsNoTracking()
            .Where(r => r.AssetId == assetId)
            .OrderByDescending(r => r.ExpiresOn.HasValue)
            .ThenByDescending(r => r.ExpiresOn)
            .ThenByDescending(r => r.ValidFrom.HasValue)
            .ThenByDescending(r => r.ValidFrom)
            .ToListAsync(cancellationToken);

        return registrations.Select(ToResponse).ToList();
    }

    public async Task<RegistrationResponse> UpdateAsync(
        Guid assetId,
        Guid registrationId,
        UpdateRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        var registration = await FindRegistrationAsync(assetId, registrationId, cancellationToken);

        registration.Kind = request.Kind!.Value;
        registration.RegistrationNumber = request.RegistrationNumber;
        registration.IssuingAuthority = request.IssuingAuthority;
        registration.ValidFrom = request.ValidFrom;
        registration.RenewedOn = request.RenewedOn;
        registration.Cost = request.Cost;
        registration.ExpiresOn = request.ExpiresOn;
        registration.Notes = request.Notes;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(registration);
    }

    public async Task DeleteAsync(
        Guid householdId, Guid assetId, Guid registrationId, CancellationToken cancellationToken = default)
    {
        var registration = await FindRegistrationAsync(assetId, registrationId, cancellationToken);
        var storageKey = registration.DocumentUrl;

        if (storageKey is not null)
        {
            var size = await fileStorageService.GetSizeAsync(storageKey, cancellationToken);
            await storageQuotaService.AdjustUsageAsync(householdId, -size, cancellationToken);
        }

        dbContext.Registrations.Remove(registration);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (storageKey is not null)
        {
            await fileStorageService.DeleteAsync(storageKey, cancellationToken);
        }
    }

    public async Task<RegistrationResponse> SetDocumentAsync(
        Guid householdId, Guid assetId, Guid registrationId, string storageKey, long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        var registration = await FindRegistrationAsync(assetId, registrationId, cancellationToken);
        var previousStorageKey = registration.DocumentUrl;
        var previousSize = previousStorageKey is not null
            ? await fileStorageService.GetSizeAsync(previousStorageKey, cancellationToken)
            : 0;

        registration.DocumentUrl = storageKey;
        await storageQuotaService.AdjustUsageAsync(householdId, sizeBytes - previousSize, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (previousStorageKey is not null)
        {
            await fileStorageService.DeleteAsync(previousStorageKey, cancellationToken);
        }

        return ToResponse(registration);
    }

    public async Task RemoveDocumentAsync(
        Guid householdId, Guid assetId, Guid registrationId, CancellationToken cancellationToken = default)
    {
        var registration = await FindRegistrationAsync(assetId, registrationId, cancellationToken);

        if (registration.DocumentUrl is { } storageKey)
        {
            var size = await fileStorageService.GetSizeAsync(storageKey, cancellationToken);
            registration.DocumentUrl = null;
            await storageQuotaService.AdjustUsageAsync(householdId, -size, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await fileStorageService.DeleteAsync(storageKey, cancellationToken);
        }
    }

    public async Task<string?> GetDocumentStorageKeyAsync(
        Guid assetId, Guid registrationId, CancellationToken cancellationToken = default)
    {
        var registration = await FindRegistrationAsync(assetId, registrationId, cancellationToken);
        return registration.DocumentUrl;
    }

    private async Task<Registration> FindRegistrationAsync(
        Guid assetId, Guid registrationId, CancellationToken cancellationToken)
    {
        return await dbContext.Registrations
            .FirstOrDefaultAsync(r => r.Id == registrationId && r.AssetId == assetId, cancellationToken)
            ?? throw new NotFoundException("Registration not found.");
    }

    private static RegistrationResponse ToResponse(Registration registration) => new(
        registration.Id,
        registration.AssetId,
        registration.Kind,
        registration.RegistrationNumber,
        registration.IssuingAuthority,
        registration.ValidFrom,
        registration.RenewedOn,
        registration.Cost,
        registration.ExpiresOn,
        registration.Notes,
        registration.DocumentUrl is not null,
        DocumentUrl: null);
}
