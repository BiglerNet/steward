using Steward.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Steward.Infrastructure.Setup;

public class SetupHostedService(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime lifetime,
    ILogger<SetupHostedService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Kestrel's own hosted service starts alongside this one. Calling StopApplication()
        // synchronously from within StartAsync races with Kestrel's still-in-flight BindAsync
        // and cancels it, crashing the process with an unhandled TaskCanceledException instead
        // of stopping cleanly. Deferring to ApplicationStarted runs this after the host (and
        // Kestrel) have fully finished starting, so StopApplication() can shut down safely.
        lifetime.ApplicationStarted.Register(() => _ = RunMigrationsAndStopAsync(cancellationToken));
        return Task.CompletedTask;
    }

    private async Task RunMigrationsAndStopAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StewardDbContext>();

            var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
            if (pending.Count == 0)
            {
                logger.LogInformation("No pending migrations. Database is up to date.");
            }
            else
            {
                logger.LogInformation("Applying {Count} pending migration(s)...", pending.Count);
                await db.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Migrations applied successfully.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Migration failed.");
            Environment.ExitCode = 1;
        }
        finally
        {
            lifetime.StopApplication();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
