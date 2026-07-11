using Steward.Application.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Steward.Infrastructure.Storage;

public static class StorageServiceExtensions
{
    public static IServiceCollection AddStewardStorage(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FileUploadOptions>(configuration.GetSection(FileUploadOptions.SectionName));
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        services.AddSingleton<IImageProcessor, SkiaImageProcessor>();
        services.AddScoped<IStorageQuotaService, StorageQuotaService>();

        return services;
    }
}
