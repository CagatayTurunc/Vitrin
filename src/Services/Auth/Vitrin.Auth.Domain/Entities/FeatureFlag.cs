using Vitrin.Shared.Kernel.Domain;

namespace Vitrin.Auth.Domain.Entities;

/// <summary>
/// Platform geneli feature flag ve A/B test konfigürasyonu.
/// Basit DB-backed yaklaşım; production'da Redis cache ile desteklenir.
/// </summary>
public sealed class FeatureFlag : Entity
{
    /// <summary>Flag anahtarı — benzersiz, uygulamada bu string ile sorgulanır.</summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>İnsan okunabilir açıklama.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Flag etkin mi?</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// A/B test rollout yüzdesi (0-100).
    /// 100 = tüm kullanıcılar, 0 = kimse, 50 = %50 rastgele.
    /// </summary>
    public int RolloutPercentage { get; private set; } = 100;

    /// <summary>
    /// Hangi role'lerin bu flag'i göreceği (boş = herkes).
    /// CSV, ör: "Admin,Maker"
    /// </summary>
    public string? AllowedRoles { get; private set; }

    /// <summary>
    /// A/B test varyantı. Null = normal flag, doluysa A/B.
    /// JSON payload — frontend bu veriyi kullanır.
    /// Örn: '{"variant":"B","headline":"Yeni Başlık"}'
    /// </summary>
    public string? VariantPayload { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }

    private FeatureFlag() { }

    public static FeatureFlag Create(
        string key,
        string description,
        bool isEnabled = false,
        int rolloutPercentage = 100,
        string? allowedRoles = null,
        string? variantPayload = null)
    {
        var now = DateTime.UtcNow;
        return new FeatureFlag
        {
            Key = key.Trim().ToLowerInvariant(),
            Description = description.Trim(),
            IsEnabled = isEnabled,
            RolloutPercentage = Math.Clamp(rolloutPercentage, 0, 100),
            AllowedRoles = allowedRoles,
            VariantPayload = variantPayload,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void Update(
        string description,
        bool isEnabled,
        int rolloutPercentage,
        string? allowedRoles,
        string? variantPayload,
        Guid updatedByUserId)
    {
        Description = description.Trim();
        IsEnabled = isEnabled;
        RolloutPercentage = Math.Clamp(rolloutPercentage, 0, 100);
        AllowedRoles = allowedRoles;
        VariantPayload = variantPayload;
        UpdatedAtUtc = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }

    /// <summary>
    /// Belirli bir userId için flag aktif mi?
    /// Rollout yüzdesi hash tabanlı deterministik olarak hesaplanır — aynı user her zaman aynı grupta.
    /// </summary>
    public bool IsActiveForUser(Guid? userId)
    {
        if (!IsEnabled) return false;
        if (RolloutPercentage >= 100) return true;
        if (RolloutPercentage <= 0) return false;
        if (userId is null) return false;

        // Deterministik hash: aynı userId her zaman aynı bucket'a düşer
        var bucket = Math.Abs(userId.Value.GetHashCode()) % 100;
        return bucket < RolloutPercentage;
    }

    public IReadOnlyList<string> GetAllowedRoles() =>
        AllowedRoles?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? [];
}
