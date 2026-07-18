using Steward.Application.Tracking.MaintenanceItems;
using Steward.Application.Tracking.MaintenanceRecurrence;
using Steward.Application.Tracking.Templates;
using Microsoft.Extensions.DependencyInjection;

namespace Steward.Infrastructure.Maintenance;

public static class MaintenanceServiceExtensions
{
    public static IServiceCollection AddStewardMaintenance(this IServiceCollection services, bool registerHostedServices = true)
    {
        services.AddScoped<IMaintenanceItemService, MaintenanceItemService>();
        services.AddScoped<IMaintenanceScheduleService, MaintenanceScheduleService>();
        services.AddScoped<ITemplateService, TemplateService>();

        if (registerHostedServices)
        {
            services.AddHostedService<PlatformTemplateSeeder>();
        }

        return services;
    }
}
