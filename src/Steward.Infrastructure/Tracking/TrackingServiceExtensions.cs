using Steward.Application.Tracking.EngineHoursLogs;
using Steward.Application.Tracking.FuelLogs;
using Steward.Application.Tracking.MileageLogs;
using Steward.Application.Tracking.Registrations;
using Steward.Application.Tracking.ServiceRecords;
using Steward.Application.Tracking.Warranties;
using Steward.Infrastructure.Tracking.EngineHoursLogs;
using Steward.Infrastructure.Tracking.FuelLogs;
using Steward.Infrastructure.Tracking.MileageLogs;
using Steward.Infrastructure.Tracking.Registrations;
using Steward.Infrastructure.Tracking.ServiceRecords;
using Steward.Infrastructure.Tracking.Warranties;
using Microsoft.Extensions.DependencyInjection;

namespace Steward.Infrastructure.Tracking;

public static class TrackingServiceExtensions
{
    public static IServiceCollection AddStewardTracking(this IServiceCollection services)
    {
        services.AddScoped<IServiceRecordService, ServiceRecordService>();
        services.AddScoped<IMileageLogService, MileageLogService>();
        services.AddScoped<IEngineHoursLogService, EngineHoursLogService>();
        services.AddScoped<IFuelLogService, FuelLogService>();
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<IWarrantyService, WarrantyService>();

        return services;
    }
}
