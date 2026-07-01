using Steward.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Steward.IntegrationTests.Infrastructure;

public class DatabaseFixture : IAsyncLifetime
{
    public IntegrationTestFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Migrate via a standalone DbContext before the web host starts so that
        // hosted services (PlatformAdminRoleSeeder, InvitationExpiryService) don't
        // race against migrations on a database that doesn't exist yet.
        var options = new DbContextOptionsBuilder<StewardDbContext>()
            .UseNpgsql(IntegrationTestFactory.ConnectionString)
            .Options;
        using (var dbContext = new StewardDbContext(options))
        {
            await dbContext.Database.MigrateAsync();
        }

        Factory = new IntegrationTestFactory();
    }

    public Task DisposeAsync()
    {
        Factory.Dispose();
        return Task.CompletedTask;
    }
}

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>;
