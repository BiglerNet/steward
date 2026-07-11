namespace Steward.Application.Storage;

public interface IFileStorageService
{
    Task<string> SaveAsync(
        Stream content, string contentType, string entityType, Guid entityId, CancellationToken cancellationToken = default);

    Task<(Stream Content, string ContentType)> OpenReadAsync(
        string storageKey, CancellationToken cancellationToken = default);

    Task<long> GetSizeAsync(string storageKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
