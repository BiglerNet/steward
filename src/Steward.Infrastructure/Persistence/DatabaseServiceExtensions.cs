using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Steward.Infrastructure.Persistence;

public static class DatabaseServiceExtensions
{
    public static IServiceCollection AddStewardDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<StewardDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        return services;
    }
}
