namespace Steward.Application.Tracking.Registrations;

public interface IRegistrationService
{
    Task<RegistrationResponse> CreateAsync(
        Guid assetId, CreateRegistrationRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RegistrationResponse>> ListAsync(
        Guid assetId, CancellationToken cancellationToken = default);

    Task<RegistrationResponse> UpdateAsync(
        Guid assetId,
        Guid registrationId,
        UpdateRegistrationRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid householdId, Guid assetId, Guid registrationId, CancellationToken cancellationToken = default);

    Task<RegistrationResponse> SetDocumentAsync(
        Guid householdId, Guid assetId, Guid registrationId, string storageKey, long sizeBytes, CancellationToken cancellationToken = default);

    Task RemoveDocumentAsync(
        Guid householdId, Guid assetId, Guid registrationId, CancellationToken cancellationToken = default);

    Task<string?> GetDocumentStorageKeyAsync(
        Guid assetId, Guid registrationId, CancellationToken cancellationToken = default);
}
