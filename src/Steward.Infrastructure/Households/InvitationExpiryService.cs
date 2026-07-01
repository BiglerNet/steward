using Steward.Application.Households;
using Steward.Domain.Enums;
using Steward.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Steward.Infrastructure.Households;

public class InvitationExpiryService(IServiceProvider serviceProvider) : IHostedService, IInvitationExpiryService
{
    private static readonly TimeSpan Period = TimeSpan.FromHours(24);
    private Timer? _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(_ => ExpireStaleInvitationsAsync().GetAwaiter().GetResult(), null, TimeSpan.Zero, Period);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public async Task ExpireStaleInvitationsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StewardDbContext>();

        var now = DateTimeOffset.UtcNow;
        await dbContext.HouseholdInvitations
            .Where(i => i.Status == InvitationStatus.Pending && i.ExpiresAt < now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.Status, InvitationStatus.Expired), cancellationToken);
    }
}
