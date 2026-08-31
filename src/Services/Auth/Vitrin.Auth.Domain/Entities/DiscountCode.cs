namespace Vitrin.Auth.Domain.Entities;

/// <summary>
/// İndirim/kupon kodu.
/// Checkout sırasında uygulanır; fiyattan yüzde veya sabit tutar düşer.
/// </summary>
public class DiscountCode
{
    public Guid Id { get; private set; }

    /// <summary>Kullanıcının gireceği kod (büyük harf, ör: LAUNCH50)</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Admin notları / açıklama</summary>
    public string? Description { get; private set; }

    public DiscountType Type { get; private set; }

    /// <summary>
    /// Yüzde indirimde 0–100 arası değer (ör: 50 → %50).
    /// Sabit indirimde TRY cinsinden tutar (ör: 100 → ₺100).
    /// </summary>
    public decimal Value { get; private set; }

    /// <summary>Hangi tier'larda geçerli. Boşsa tüm tier'larda geçerli.</summary>
    public List<SubscriptionTier> ApplicableTiers { get; private set; } = [];

    /// <summary>Toplam kaç kez kullanılabilir. null = sınırsız.</summary>
    public int? MaxUses { get; private set; }

    /// <summary>Bir kullanıcı kaç kez kullanabilir. Genellikle 1.</summary>
    public int MaxUsesPerUser { get; private set; }

    /// <summary>Kaç yenileme dönemine uygulanır. null = sadece ilk ödeme.</summary>
    public int? DurationMonths { get; private set; }

    public int CurrentUseCount { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime? StartsAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedByAdminId { get; private set; }

    // Navigation
    public ICollection<DiscountCodeUsage> Usages { get; private set; } = [];

    protected DiscountCode() { }

    private DiscountCode(
        Guid id,
        string code,
        string? description,
        DiscountType type,
        decimal value,
        List<SubscriptionTier> applicableTiers,
        int? maxUses,
        int maxUsesPerUser,
        int? durationMonths,
        DateTime? startsAt,
        DateTime? expiresAt,
        Guid createdByAdminId)
    {
        Id = id;
        Code = code.ToUpperInvariant().Trim();
        Description = description;
        Type = type;
        Value = value;
        ApplicableTiers = applicableTiers;
        MaxUses = maxUses;
        MaxUsesPerUser = maxUsesPerUser;
        DurationMonths = durationMonths;
        StartsAt = startsAt;
        ExpiresAt = expiresAt;
        CreatedByAdminId = createdByAdminId;
        IsActive = true;
        CurrentUseCount = 0;
        CreatedAt = DateTime.UtcNow;
    }

    public static DiscountCode Create(
        string code,
        string? description,
        DiscountType type,
        decimal value,
        List<SubscriptionTier>? applicableTiers,
        int? maxUses,
        int maxUsesPerUser,
        int? durationMonths,
        DateTime? startsAt,
        DateTime? expiresAt,
        Guid createdByAdminId)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Kupon kodu boş olamaz.");

        if (type == DiscountType.Percentage && (value <= 0 || value > 100))
            throw new ArgumentException("Yüzde indirim 0–100 arasında olmalıdır.");

        if (type == DiscountType.FixedAmount && value <= 0)
            throw new ArgumentException("Sabit indirim tutarı sıfırdan büyük olmalıdır.");

        if (maxUsesPerUser < 1)
            throw new ArgumentException("Kullanıcı başına kullanım limiti en az 1 olmalıdır.");

        return new DiscountCode(
            Guid.NewGuid(),
            code,
            description,
            type,
            value,
            applicableTiers ?? [],
            maxUses,
            maxUsesPerUser,
            durationMonths,
            startsAt,
            expiresAt,
            createdByAdminId);
    }

    /// <summary>
    /// Kodun şu an kullanılabilir olup olmadığını kontrol eder.
    /// </summary>
    public DiscountValidationResult Validate(Guid userId, SubscriptionTier tier, int userUseCount)
    {
        if (!IsActive)
            return DiscountValidationResult.Fail("Kupon kodu aktif değil.");

        var now = DateTime.UtcNow;

        if (StartsAt.HasValue && StartsAt.Value > now)
            return DiscountValidationResult.Fail("Kupon henüz geçerli değil.");

        if (ExpiresAt.HasValue && ExpiresAt.Value < now)
            return DiscountValidationResult.Fail("Kupon süresi dolmuş.");

        if (MaxUses.HasValue && CurrentUseCount >= MaxUses.Value)
            return DiscountValidationResult.Fail("Kupon kullanım limiti dolmuş.");

        if (userUseCount >= MaxUsesPerUser)
            return DiscountValidationResult.Fail("Bu kuponu daha önce kullandınız.");

        if (ApplicableTiers.Count > 0 && !ApplicableTiers.Contains(tier))
            return DiscountValidationResult.Fail($"Bu kupon {tier} planı için geçerli değil.");

        return DiscountValidationResult.Ok(CalculateDiscount(tier));
    }

    /// <summary>
    /// İndirim sonrası ödeme tutarını hesaplar.
    /// </summary>
    public decimal CalculateDiscount(SubscriptionTier tier)
    {
        var originalPrice = GetTierPrice(tier);

        var discount = Type switch
        {
            DiscountType.Percentage => originalPrice * (Value / 100m),
            DiscountType.FixedAmount => Math.Min(Value, originalPrice), // fiyatı negatife düşürme
            _ => 0m
        };

        return Math.Round(discount, 2);
    }

    public decimal GetFinalPrice(SubscriptionTier tier)
    {
        var originalPrice = GetTierPrice(tier);
        return Math.Max(0m, originalPrice - CalculateDiscount(tier));
    }

    public void IncrementUseCount()
    {
        CurrentUseCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(
        string? description,
        int? maxUses,
        DateTime? expiresAt)
    {
        if (description is not null) Description = description;
        if (maxUses is not null) MaxUses = maxUses;
        if (expiresAt is not null) ExpiresAt = expiresAt;
        UpdatedAt = DateTime.UtcNow;
    }

    private static decimal GetTierPrice(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.ProMaker => 299m,
        SubscriptionTier.Enterprise => 999m,
        _ => 0m
    };
}

/// <summary>
/// Bir kullanıcının bir kuponu kullandığının kaydı.
/// Idempotency ve per-user limit kontrolü için kullanılır.
/// </summary>
public class DiscountCodeUsage
{
    public Guid Id { get; private set; }
    public Guid DiscountCodeId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? PaymentHistoryId { get; private set; }
    public decimal DiscountApplied { get; private set; }
    public DateTime UsedAt { get; private set; }

    // Navigation
    public DiscountCode? DiscountCode { get; private set; }

    protected DiscountCodeUsage() { }

    public static DiscountCodeUsage Create(
        Guid discountCodeId,
        Guid userId,
        decimal discountApplied)
    {
        return new DiscountCodeUsage
        {
            Id = Guid.NewGuid(),
            DiscountCodeId = discountCodeId,
            UserId = userId,
            DiscountApplied = discountApplied,
            UsedAt = DateTime.UtcNow
        };
    }

    public void LinkToPayment(Guid paymentHistoryId)
    {
        PaymentHistoryId = paymentHistoryId;
    }
}

public record DiscountValidationResult(
    bool IsValid,
    decimal DiscountAmount,
    string? ErrorMessage)
{
    public static DiscountValidationResult Ok(decimal discountAmount) =>
        new(true, discountAmount, null);

    public static DiscountValidationResult Fail(string error) =>
        new(false, 0, error);
}

public enum DiscountType
{
    /// <summary>Yüzde indirim (ör: %50)</summary>
    Percentage = 0,

    /// <summary>Sabit tutar indirimi (ör: ₺100)</summary>
    FixedAmount = 1
}
