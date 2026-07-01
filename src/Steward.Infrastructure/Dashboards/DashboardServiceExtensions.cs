using Steward.Application.Dashboards;
using Microsoft.Extensions.DependencyInjection;

namespace Steward.Infrastructure.Dashboards;

public static class DashboardServiceExtensions
{
    public static IServiceCollection AddStewardDashboards(this IServiceCollection services)
    {
        services.AddScoped<IDashboardService, DashboardService>();
        return services;
    }
}
