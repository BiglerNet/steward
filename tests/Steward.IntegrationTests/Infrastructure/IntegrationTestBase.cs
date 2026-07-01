using System.Net.Http.Headers;
using Steward.Domain.Entities;
using Steward.Domain.Entities.Assets;
using Steward.Domain.Enums;
using Steward.Infrastructure.Identity;
using Steward.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Steward.IntegrationTests.Infrastructure;

[Collection("Database collection")]
public abstract class IntegrationTestBase(DatabaseFixture fixture)
{
    protected IntegrationTestFactory Factory => fixture.Factory;

    // Household.CreatedByUserId has an FK to AspNetUsers, so every household needs a
    // real ApplicationUser row; the value need not match any authenticated test actor.
    protected async Task<Guid> CreateHouseholdAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StewardDbContext>();

        var creator = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"test-{Guid.NewGuid():N}@example.com",
            NormalizedUserName = $"TEST-{Guid.NewGuid():N}@EXAMPLE.COM",
            Email = $"test-{Guid.NewGuid():N}@example.com",
        };
        dbContext.Users.Add(creator);

        var household = new Household
        {
            Id = Guid.NewGuid(),
            Name = $"Test Household {Guid.NewGuid()}",
            PublicSlug = $"test-{Guid.NewGuid():N}",
            IsPublicVisible = false,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = creator.Id,
        };

        dbContext.Households.Add(household);
        await dbContext.SaveChangesAsync();

        return household.Id;
    }

    // HouseholdMembership.UserId has an FK to AspNetUsers, so the member also needs a
    // real ApplicationUser row matching the id used to authenticate as that actor.
    protected async Task AddMemberAsync(Guid householdId, Guid userId, HouseholdMemberRole role)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StewardDbContext>();

        dbContext.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = $"test-{Guid.NewGuid():N}@example.com",
            NormalizedUserName = $"TEST-{Guid.NewGuid():N}@EXAMPLE.COM",
            Email = $"test-{Guid.NewGuid():N}@example.com",
        });

        dbContext.HouseholdMemberships.Add(new HouseholdMembership
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            UserId = userId,
            Role = role,
            Status = HouseholdMemberStatus.Active,
            InvitedAt = DateTimeOffset.UtcNow,
            AcceptedAt = DateTimeOffset.UtcNow,
        });

        await dbContext.SaveChangesAsync();
    }

    protected async Task<(Guid HouseholdId, Guid UserId)> CreateHouseholdWithMemberAsync(HouseholdMemberRole role)
    {
        var householdId = await CreateHouseholdAsync();
        var userId = Guid.NewGuid();
        await AddMemberAsync(householdId, userId, role);
        return (householdId, userId);
    }

    protected HttpClient CreateAuthenticatedClient(Guid userId, IEnumerable<string>? roles = null)
    {
        var client = Factory.CreateClient();
        var token = TestJwt.Create(userId, roles: roles);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected HttpClient CreateAnonymousClient() => Factory.CreateClient();

    protected async Task<Guid> CreateAssetAsync(Guid householdId, string name = "Test Snowmobile")
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StewardDbContext>();

        var now = DateTimeOffset.UtcNow;
        var asset = new Snowmobile
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            Name = name,
            UsageTrackingMode = UsageTrackingMode.Hours,
            CreatedAt = now,
            UpdatedAt = now,
        };

        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync();

        return asset.Id;
    }

    protected async Task<Guid> CreateEngineAsync(Guid assetId, string label = "Test Engine")
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StewardDbContext>();

        var engine = new Engine
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            Label = label,
            EngineType = EngineType.Ice,
            FuelType = FuelType.Gasoline,
            Status = EngineStatus.Active,
        };

        dbContext.Engines.Add(engine);
        await dbContext.SaveChangesAsync();

        return engine.Id;
    }
}
