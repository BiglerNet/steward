using Steward.Application.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace Steward.Infrastructure.Identity;

public class MemoryCacheOAuthExchangeService(IMemoryCache cache) : IOAuthExchangeService
{
    private static readonly TimeSpan CodeTtl = TimeSpan.FromSeconds(60);

    public string GenerateCode(Guid userId)
    {
        var code = Guid.NewGuid().ToString("N");
        cache.Set(CacheKey(code), userId, CodeTtl);
        return code;
    }

    public bool TryRedeemCode(string code, out Guid userId)
    {
        var key = CacheKey(code);
        if (cache.TryGetValue(key, out Guid storedUserId))
        {
            cache.Remove(key);
            userId = storedUserId;
            return true;
        }

        userId = Guid.Empty;
        return false;
    }

    private static string CacheKey(string code) => $"oauth-exchange:{code}";
}
