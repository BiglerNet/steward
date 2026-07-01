using Steward.Domain.Common.Exceptions;
using Steward.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;

namespace Steward.UnitTests.Storage;

public class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), "mt-storage-tests", Guid.NewGuid().ToString("N"));
    private readonly LocalFileStorageService _service;

    public LocalFileStorageServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:RootPath"] = _rootPath })
            .Build();

        _service = new LocalFileStorageService(configuration);
    }

    [Fact]
    public async Task SaveAsync_Generates_Key_With_EntityType_EntityId_And_Extension()
    {
        var entityId = Guid.NewGuid();
        using var content = new MemoryStream("hello"u8.ToArray());

        var storageKey = await _service.SaveAsync(content, "application/pdf", "registrations", entityId, TestContext.Current.CancellationToken);

        Assert.StartsWith($"registrations/{entityId}/", storageKey);
        Assert.EndsWith(".pdf", storageKey);
    }

    [Fact]
    public async Task SaveAsync_Creates_Directories_As_Needed()
    {
        var entityId = Guid.NewGuid();
        using var content = new MemoryStream("hello"u8.ToArray());

        await _service.SaveAsync(content, "image/png", "warranties", entityId, TestContext.Current.CancellationToken);

        var entityDir = Path.Combine(_rootPath, "warranties", entityId.ToString());
        Assert.True(Directory.Exists(entityDir));
    }

    [Fact]
    public async Task Save_Read_Delete_Round_Trip()
    {
        var entityId = Guid.NewGuid();
        var bytes = "round trip content"u8.ToArray();
        using var content = new MemoryStream(bytes);

        var storageKey = await _service.SaveAsync(content, "application/pdf", "registrations", entityId, TestContext.Current.CancellationToken);

        var (readStream, contentType) = await _service.OpenReadAsync(storageKey, TestContext.Current.CancellationToken);
        using var memoryStream = new MemoryStream();
        await readStream.CopyToAsync(memoryStream, TestContext.Current.CancellationToken);
        readStream.Dispose();

        Assert.Equal("application/pdf", contentType);
        Assert.Equal(bytes, memoryStream.ToArray());

        await _service.DeleteAsync(storageKey, TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.OpenReadAsync(storageKey, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task OpenReadAsync_Throws_NotFoundException_When_Missing()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.OpenReadAsync("registrations/missing/missing.pdf", TestContext.Current.CancellationToken));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
