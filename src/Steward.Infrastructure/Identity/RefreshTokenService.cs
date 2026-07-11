using System.Security.Cryptography;
using System.Text;
using Steward.Application.Identity;
using Steward.Domain.Entities;
using Steward.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace Steward.Infrastructure.Identity;

public class RefreshTokenService(
    StewardDbContext dbContext,
    IConfiguration configuration,
    IMemoryCache cache) : IRefreshTokenService
{
    public async Task<RefreshTokenIssueResult> IssueAsync(Guid userId, bool rememberMe, CancellationToken cancellationToken = default)
    {
        var rawToken = GenerateRawToken();
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.Add(GetExpiry(rememberMe));

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Hash(rawToken),
            ExpiresAt = expiresAt,
            RememberMe = rememberMe,
            CreatedAt = now,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RefreshTokenIssueResult(rawToken, expiresAt);
    }

    public async Task<RefreshTokenRotateResult?> RotateAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var hash = Hash(refreshToken);
        var record = await dbContext.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (record is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;

        if (record.RevokedAt is not null)
        {
            // Already rotated. Within the grace window, a cached copy of the pair produced by
            // the original rotation lets benign races (multiple tabs, retries) succeed without
            // erroring or rotating again — we can't recompute the raw token from its stored hash,
            // so a cache miss inside the window (e.g. a process restart) fails closed as theft.
            if (now <= record.RevokedAt.Value.Add(GetGracePeriod()) &&
                cache.TryGetValue<RefreshTokenRotateResult>(GraceCacheKey(hash), out var cached))
            {
                return cached;
            }

            await RevokeChainInternalAsync(record.UserId, now, cancellationToken);
            return null;
        }

        if (record.ExpiresAt <= now)
        {
            return null;
        }

        var rawToken = GenerateRawToken();
        var newHash = Hash(rawToken);
        var expiresAt = now.Add(GetExpiry(record.RememberMe));

        record.RevokedAt = now;
        record.ReplacedByTokenHash = newHash;

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = record.UserId,
            TokenHash = newHash,
            ExpiresAt = expiresAt,
            RememberMe = record.RememberMe,
            CreatedAt = now,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var result = new RefreshTokenRotateResult(record.UserId, rawToken, expiresAt);
        cache.Set(GraceCacheKey(hash), result, GetGracePeriod());
        return result;
    }

    public async Task RevokeChainAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var hash = Hash(refreshToken);
        var record = await dbContext.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (record is null)
        {
            return;
        }

        await RevokeChainInternalAsync(record.UserId, DateTimeOffset.UtcNow, cancellationToken);
    }

    private async Task RevokeChainInternalAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private TimeSpan GetExpiry(bool rememberMe) =>
        rememberMe
            ? configuration.GetValue<TimeSpan?>("Jwt:RefreshToken:RememberMeExpiry") ?? TimeSpan.FromDays(30)
            : configuration.GetValue<TimeSpan?>("Jwt:RefreshToken:DefaultExpiry") ?? TimeSpan.FromHours(10);

    private TimeSpan GetGracePeriod() =>
        configuration.GetValue<TimeSpan?>("Jwt:RefreshToken:ReuseGracePeriod") ?? TimeSpan.FromSeconds(45);

    private static string GraceCacheKey(string oldTokenHash) => $"refresh-grace:{oldTokenHash}";

    private static string GenerateRawToken() => RandomNumberGenerator.GetHexString(64);

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
