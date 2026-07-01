using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Steward.Application;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceExtensions).Assembly);

        services.Scan(scan => scan
            .FromAssemblies(typeof(ApplicationServiceExtensions).Assembly)
            .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Service")))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }
}
