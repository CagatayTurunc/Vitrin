using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vitrin.Product.Infrastructure.Data;
using Vitrin.Shared.Contracts.Subscription;

namespace Vitrin.Product.Infrastructure.Services;

/// <summary>
/// Product service'teki quota implementasyonu.
/// Ürün sayısını kendi DB'sinden okur, tier bilgisini Auth service'ten HTTP ile alır.
/// </summary>
public sealed class ProductSubscriptionQuotaService : ISubscriptionQuotaService
{
    private readonly ProductDbContext _db;
    private readonly HttpClient _authHttpClient;
    private readonly ILogger<ProductSubscriptionQuotaService> _logger;

    // Tier limitleri
    private const int FreeProductLimit = 5;
    private const int FreeCollectionLimit = 1;

    public ProductSubscriptionQuotaService(
        ProductDbContext db,
        IHttpClientFactory httpClientFactory,
        ILogger<ProductSubscriptionQuotaService> logger)
    {
        _db = db;
        _authHttpClient = httpClientFactory.CreateClient("AuthService");
        _logger = logger;
    }

    public async Task<QuotaCheckResult> CanCreateProductAsync(Guid userId, CancellationToken ct = default)
    {
        var tier = await GetUserTierAsync(userId, ct);

        // Pro ve Enterprise: sınırsız
        if (tier >= SubscriptionTier.ProMaker)
            return QuotaCheckResult.Allow(0, -1);

        // Free: 5 ürün limiti
        var activeProductCount = await _db.Products
            .CountAsync(p => p.MakerId == userId &&
                        p.Status != Vitrin.Product.Domain.Entities.ProductStatus.Archived,
                        ct);

        if (activeProductCount >= FreeProductLimit)
        {
            return QuotaCheckResult.Deny(
                $"Ücretsiz planda en fazla {FreeProductLimit} ürün paylaşabilirsiniz. " +
                "Pro Maker planına geçerek sınırsız ürün ekleyebilirsiniz.",
                SubscriptionTier.ProMaker,
                activeProductCount,
                FreeProductLimit);
        }

        return QuotaCheckResult.Allow(activeProductCount, FreeProductLimit);
    }

    public async Task<QuotaCheckResult> CanCreateCollectionAsync(Guid userId, CancellationToken ct = default)
    {
        var tier = await GetUserTierAsync(userId, ct);

        if (tier == SubscriptionTier.Enterprise)
            return QuotaCheckResult.Allow(0, -1);

        var limit = tier == SubscriptionTier.ProMaker ? 10 : FreeCollectionLimit;

        var count = await _db.Collections
            .CountAsync(c => c.UserId == userId, ct);

        if (count >= limit)
        {
            return QuotaCheckResult.Deny(
                $"Bu planda en fazla {limit} koleksiyon oluşturabilirsiniz.",
                tier == SubscriptionTier.Free ? SubscriptionTier.ProMaker : SubscriptionTier.Enterprise,
                count,
                limit);
        }

        return QuotaCheckResult.Allow(count, limit);
    }

    public async Task<QuotaCheckResult> CanAddTeamMemberAsync(Guid userId, CancellationToken ct = default)
    {
        var tier = await GetUserTierAsync(userId, ct);

        if (tier == SubscriptionTier.Free)
        {
            return QuotaCheckResult.Deny(
                "Takım üyesi eklemek için Pro Maker planına geçmeniz gerekiyor.",
                SubscriptionTier.ProMaker,
                0,
                0);
        }

        return QuotaCheckResult.Allow(0, tier == SubscriptionTier.Enterprise ? 10 : 3);
    }

    public async Task<QuotaCheckResult> CanScheduleLaunchAsync(
        Guid userId,
        DateTime launchDate,
        CancellationToken ct = default)
    {
        var tier = await GetUserTierAsync(userId, ct);

        if (tier == SubscriptionTier.Free)
        {
            return QuotaCheckResult.Deny(
                "Lansman planlaması için Pro Maker planına geçmeniz gerekiyor.",
                SubscriptionTier.ProMaker,
                0,
                0);
        }

        var maxDaysAhead = tier == SubscriptionTier.Enterprise ? 30 : 7;
        var daysAhead = (launchDate - DateTime.UtcNow).TotalDays;

        if (daysAhead > maxDaysAhead)
        {
            return QuotaCheckResult.Deny(
                $"Bu planla en fazla {maxDaysAhead} gün ilerisi için planlama yapabilirsiniz.",
                SubscriptionTier.Enterprise,
                (int)daysAhead,
                maxDaysAhead);
        }

        return QuotaCheckResult.Allow((int)daysAhead, maxDaysAhead);
    }

    public Task<int> GetRemainingAiQuotaAsync(Guid userId, CancellationToken ct = default)
    {
        // AI quota SQLite'ta Auth service'te tutuluyor — buraya 0 dönüyoruz
        // AI service kendi kontrolünü yapıyor
        return Task.FromResult(0);
    }

    public async Task<SubscriptionTier> GetUserTierAsync(Guid userId, CancellationToken ct = default)
    {
        // MakerTierSnapshot'ı maker'ın aktif ürünlerinden oku (denormalized data)
        // Daha performanslı — Auth service'e HTTP call gerektirmez
        var snapshot = await _db.Products
            .Where(p => p.MakerId == userId)
            .Select(p => p.MakerTierSnapshot)
            .FirstOrDefaultAsync(ct);

        if (snapshot == null)
        {
            // Hiç ürünü yoksa Auth service'e sor
            return await GetTierFromAuthServiceAsync(userId, ct);
        }

        return snapshot switch
        {
            "ProMaker" => SubscriptionTier.ProMaker,
            "Enterprise" => SubscriptionTier.Enterprise,
            _ => SubscriptionTier.Free
        };
    }

    public Task<SubscriptionInfo> GetSubscriptionInfoAsync(Guid userId, CancellationToken ct = default)
    {
        // Bu endpoint Auth service'te implement edilmeli, buraya stub dönüyoruz
        var info = new SubscriptionInfo(
            SubscriptionTier.Free,
            SubscriptionStatus.Active,
            DateTime.UtcNow.AddYears(100),
            false,
            false,
            new QuotaUsage(0, FreeProductLimit, 0, FreeCollectionLimit, 0, 0, 0, 5));
        return Task.FromResult(info);
    }

    private async Task<SubscriptionTier> GetTierFromAuthServiceAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            var response = await _authHttpClient.GetAsync(
                $"/api/subscription/tier/{userId}", ct);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                // JSON: { "tier": "ProMaker" }
                using var doc = System.Text.Json.JsonDocument.Parse(content);
                var tierStr = doc.RootElement
                    .GetProperty("tier")
                    .GetString();

                return tierStr switch
                {
                    "ProMaker" => SubscriptionTier.ProMaker,
                    "Enterprise" => SubscriptionTier.Enterprise,
                    _ => SubscriptionTier.Free
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Auth service tier check failed for user {UserId}, defaulting to Free", userId);
        }

        return SubscriptionTier.Free;
    }
}
