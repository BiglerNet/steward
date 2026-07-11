using Steward.Application.Identity;
using Steward.Infrastructure.Identity;
using Steward.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace Steward.UnitTests.Identity;

public class RefreshTokenServiceTests
{
    private readonly StewardDbContext _dbContext;
    private readonly IMemoryCache _cache;

    public RefreshTokenServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<StewardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new StewardDbContext(dbOptions);
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    private RefreshTokenService CreateService(string reuseGracePeriod = "00:00:45")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:RefreshToken:RememberMeExpiry"] = "30.00:00:00",
                ["Jwt:RefreshToken:DefaultExpiry"] = "10:00:00",
                ["Jwt:RefreshToken:ReuseGracePeriod"] = reuseGracePeriod,
            })
            .Build();

        return new RefreshTokenService(_dbContext, config, _cache);
    }

    [Fact]
    public async Task IssueAsync_StoresOnlyHash_NotRawToken()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();

        var issued = await service.IssueAsync(userId, rememberMe: true, TestContext.Current.CancellationToken);

        var stored = await _dbContext.RefreshTokens.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(userId, stored.UserId);
        Assert.NotEqual(issued.Token, stored.TokenHash);
        Assert.DoesNotContain(issued.Token, stored.TokenHash);
    }

    [Fact]
    public async Task RotateAsync_ValidToken_RevokesOldAndLinksNew()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();
        var issued = await service.IssueAsync(userId, rememberMe: true, TestContext.Current.CancellationToken);

        var rotated = await service.RotateAsync(issued.Token, TestContext.Current.CancellationToken);

        Assert.NotNull(rotated);
        Assert.Equal(userId, rotated!.UserId);
        Assert.NotEqual(issued.Token, rotated.Token);

        var tokens = await _dbContext.RefreshTokens.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, tokens.Count);
        var oldRecord = tokens.Single(t => t.RevokedAt is not null);
        var newRecord = tokens.Single(t => t.RevokedAt is null);
        Assert.Equal(newRecord.TokenHash, oldRecord.ReplacedByTokenHash);
    }

    [Fact]
    public async Task RotateAsync_ReuseWithinGraceWindow_ReturnsSamePair()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();
        var issued = await service.IssueAsync(userId, rememberMe: true, TestContext.Current.CancellationToken);

        var firstRotation = await service.RotateAsync(issued.Token, TestContext.Current.CancellationToken);
        var secondRotation = await service.RotateAsync(issued.Token, TestContext.Current.CancellationToken);

        Assert.NotNull(firstRotation);
        Assert.NotNull(secondRotation);
        Assert.Equal(firstRotation!.Token, secondRotation!.Token);
        Assert.Equal(firstRotation.ExpiresAt, secondRotation.ExpiresAt);

        var tokens = await _dbContext.RefreshTokens.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, tokens.Count);
    }

    [Fact]
    public async Task RotateAsync_ReuseOutsideGraceWindow_RevokesFullChain()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();
        var issued = await service.IssueAsync(userId, rememberMe: true, TestContext.Current.CancellationToken);
        await service.RotateAsync(issued.Token, TestContext.Current.CancellationToken);

        var oldRecord = await _dbContext.RefreshTokens.SingleAsync(t => t.RevokedAt != null, TestContext.Current.CancellationToken);
        oldRecord.RevokedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await service.RotateAsync(issued.Token, TestContext.Current.CancellationToken);

        Assert.Null(result);
        var tokens = await _dbContext.RefreshTokens.Where(t => t.UserId == userId).ToListAsync(TestContext.Current.CancellationToken);
        Assert.All(tokens, t => Assert.NotNull(t.RevokedAt));
    }

    [Fact]
    public async Task RotateAsync_ExpiredToken_ReturnsNull()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();
        var issued = await service.IssueAsync(userId, rememberMe: true, TestContext.Current.CancellationToken);

        var record = await _dbContext.RefreshTokens.SingleAsync(TestContext.Current.CancellationToken);
        record.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await service.RotateAsync(issued.Token, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task RotateAsync_UnknownToken_ReturnsNull()
    {
        var service = CreateService();

        var result = await service.RotateAsync("unknown-token", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task RevokeChainAsync_RevokesAllActiveTokensForUser()
    {
        var service = CreateService();
        var userId = Guid.NewGuid();
        var issued = await service.IssueAsync(userId, rememberMe: true, TestContext.Current.CancellationToken);

        await service.RevokeChainAsync(issued.Token, TestContext.Current.CancellationToken);

        var record = await _dbContext.RefreshTokens.SingleAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(record.RevokedAt);

        var result = await service.RotateAsync(issued.Token, TestContext.Current.CancellationToken);
        Assert.Null(result);
    }
}
