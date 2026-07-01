using Steward.Application.Storage;
using Steward.Domain.Common.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Steward.Infrastructure.Storage;

public class LocalFileStorageService(IConfiguration configuration) : IFileStorageService
{
    private static readonly Dictionary<string, string> ContentTypeToExtension = new()
    {
        ["application/pdf"] = ".pdf",
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
    };

    private static readonly Dictionary<string, string> ExtensionToContentType =
        ContentTypeToExtension.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

    private string RootPath => configuration["Storage:RootPath"]
        ?? throw new InvalidOperationException("Storage:RootPath is required.");

    public async Task<string> SaveAsync(
        Stream content, string contentType, string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        var extension = ContentTypeToExtension.GetValueOrDefault(contentType, string.Empty);
        var storageKey = $"{entityType}/{entityId}/{Guid.NewGuid()}{extension}";
        var fullPath = ResolvePath(storageKey);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, cancellationToken);

        return storageKey;
    }

    public Task<(Stream Content, string ContentType)> OpenReadAsync(
        string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(storageKey);
        if (!File.Exists(fullPath))
        {
            throw new NotFoundException("Document not found.");
        }

        var contentType = ExtensionToContentType.GetValueOrDefault(Path.GetExtension(fullPath), "application/octet-stream");
        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult((stream, contentType));
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(storageKey);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        var fullPath = Path.GetFullPath(Path.Combine(RootPath, storageKey));
        var rootFullPath = Path.GetFullPath(RootPath);
        if (!fullPath.StartsWith(rootFullPath, StringComparison.Ordinal))
        {
            throw new BadRequestException("Invalid storage key.");
        }

        return fullPath;
    }
}
