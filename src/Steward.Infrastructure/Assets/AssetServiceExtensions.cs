using Steward.Application.Assets;
using Steward.Application.Assets.Engines;
using Steward.Infrastructure.Assets.Engines;
using Microsoft.Extensions.DependencyInjection;

namespace Steward.Infrastructure.Assets;

public static class AssetServiceExtensions
{
    public static IServiceCollection AddStewardAssets(this IServiceCollection services)
    {
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IEngineService, EngineService>();

        return services;
    }
}
