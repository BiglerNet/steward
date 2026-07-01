using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Steward.Infrastructure.Identity;

public class PlatformAdminRoleSeeder(IServiceProvider serviceProvider) : IHostedService
{
    public const string RoleName = "PlatformAdmin";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        if (!await roleManager.RoleExistsAsync(RoleName))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(RoleName));
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
