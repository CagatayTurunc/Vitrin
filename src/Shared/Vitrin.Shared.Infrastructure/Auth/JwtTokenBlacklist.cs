using StackExchange.Redis;
using Microsoft.Extensions.DependencyInjection;

namespace Vitrin.Shared.Infrastructure.Auth;

/// <summary>
/// Madde 4 — Çıkışta oturumu düşür:
/// Logout olan ya da admin tarafından ban'lanan kullanıcıların JWT token'larını
/// Redis'te kara listeye alır. Token süresi dolana kadar geçersiz sayılır.
///
/// Neden gerekli?
/// JWT'ler stateless'tır — bir kez verilince backend iptal edemez.
/// Blacklist olmadan logout olan kullanıcı token süresi dolana kadar API'ye erişebilir.
/// 1 saatlik kısa token ömrüyle birlikte bu risk minimuma iner.
/// </summary>
public interface IJwtTokenBlacklist
{
    /// <summary>Bir token JTI'sini kara listeye ekler.</summary>
    /// <param name="jti">JWT'deki "jti" claim değeri (benzersiz token ID)</param>
    /// <param name="expiry">Token'ın asıl sona erme süresi — bu süreden sonra Redis key'i otomatik temizlenir</param>
    Task BlacklistAsync(string jti, TimeSpan expiry, CancellationToken cancellationToken = default);

    /// <summary>Token'ın kara listede olup olmadığını kontrol eder.</summary>
    Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken = default);
}

public sealed class RedisJwtTokenBlacklist : IJwtTokenBlacklist
{
    private readonly IDatabase _db;

    // Prefix ile diğer Redis key'lerden ayrılır
    private static string Key(string jti) => $"vitrin:jwt:blacklist:{jti}";

    public RedisJwtTokenBlacklist(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task BlacklistAsync(string jti, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        // Value önemli değil, key'in TTL'i token süresiyle eşleşiyor.
        // Token expire olunca Redis key'i de otomatik silinir — bellek sızıntısı yok.
        await _db.StringSetAsync(Key(jti), "1", expiry);
    }

    public async Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken = default)
    {
        return await _db.KeyExistsAsync(Key(jti));
    }
}

public static class JwtTokenBlacklistExtensions
{
    public static IServiceCollection AddVitrinTokenBlacklist(this IServiceCollection services)
    {
        services.AddSingleton<IJwtTokenBlacklist, RedisJwtTokenBlacklist>();
        return services;
    }
}
