using Steward.Application.VinDecode;
using Microsoft.Extensions.DependencyInjection;

namespace Steward.Infrastructure.VinDecode;

public static class VinDecodeServiceExtensions
{
    public static IServiceCollection AddStewardVinDecode(this IServiceCollection services)
    {
        services.AddHttpClient<IVinDecodeService, VpicVinDecodeService>(client =>
        {
            client.BaseAddress = new Uri("https://vpic.nhtsa.dot.gov/api/vehicles/");
            client.Timeout = TimeSpan.FromSeconds(8);
        });

        return services;
    }
}
