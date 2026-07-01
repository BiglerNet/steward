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

    Task DeleteAsync(Guid assetId, Guid registrationId, CancellationToken cancellationToken = default);

    Task<RegistrationResponse> SetDocumentAsync(
        Guid assetId, Guid registrationId, string storageKey, CancellationToken cancellationToken = default);

    Task RemoveDocumentAsync(Guid assetId, Guid registrationId, CancellationToken cancellationToken = default);

    Task<string?> GetDocumentStorageKeyAsync(
        Guid assetId, Guid registrationId, CancellationToken cancellationToken = default);
}
