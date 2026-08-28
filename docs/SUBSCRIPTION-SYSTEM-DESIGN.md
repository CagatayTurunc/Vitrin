# Vitrin Subscription System — Tasarım Dokümanı

> Tarih: 27 Ağustos 2026
> Durum: Tasarım aşaması
> Hedef: Freemium business model implementasyonu

---

## 1. Business Model — Freemium Tier Yapısı

### Plan Tiers

```csharp
public enum SubscriptionTier
{
    Free = 0,
    ProMaker = 1,      // ₺299/ay
    Enterprise = 2      // ₺999/ay
}
```

### Özellik Matrisi

| Özellik | Free | Pro Maker | Enterprise |
|---------|------|-----------|------------|
| **Temel Limitler** |
| Ürün sayısı | 5 | Sınırsız | Sınırsız |
| AI analiz kotası | 5/gün | 50/gün | 200/gün |
| Koleksiyon sayısı | 1 | 10 | Sınırsız |
| Takım üyeleri | 0 | 3 | 10 |
| **Analytics & Insights** |
| Veri saklama | 7 gün | 90 gün | 1 yıl |
| Export (CSV/JSON) | ❌ | ✅ | ✅ |
| Referrer tracking | Temel | Gelişmiş | Full + UTM |
| A/B testing | ❌ | ❌ | ✅ |
| **Görünürlük & Branding** |
| Platform badge | ❌ | "Pro" badge | "Enterprise" badge |
| Profil öne çıkarma | ❌ | ❌ | ✅ Anasayfa |
| Watermark | Vitrin logo | ❌ Yok | ❌ Yok |
| **Launch & Zamanlama** |
| Scheduled launch | ❌ | 7 gün önceden | 30 gün önceden |
| Launch countdown | ❌ | ✅ | ✅ |
| Pre-launch page | ❌ | ✅ | ✅ + özel tasarım |
| **Community Features** |
| Comment pinning | ❌ | Kendi ürününde | Tüm yorumlarda |
| Custom slug | Otomatik | ✅ | ✅ |
| Priority in search | ❌ | ✅ | ✅✅ |
| **Integrations** |
| Webhook | ❌ | ❌ | ✅ |
| API access | ❌ | 1K/gün | 10K/gün |
| Zapier | ❌ | ❌ | ✅ |
| **Destek** |
| Community forum | ✅ | ✅ | ✅ |
| Email support | ❌ | 48 saat | 12 saat |
| Video call | ❌ | ❌ | ✅ Aylık |

---

## 2. Database Schema

### 2.1 Subscription Entity (Auth Service)

```csharp
public class Subscription : AggregateRoot
{
    public Guid UserId { get; private set; }
    public SubscriptionTier Tier { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    
    // Billing cycle
    public DateTime CurrentPeriodStart { get; private set; }
    public DateTime CurrentPeriodEnd { get; private set; }
    
    // Payment
    public string? StripeCustomerId { get; private set; }
    public string? StripeSubscriptionId { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    
    // Trial
    public DateTime? TrialEndsAt { get; private set; }
    public bool IsTrialing => TrialEndsAt.HasValue && TrialEndsAt.Value > DateTime.UtcNow;
    
    // Cancellation
    public bool CancelAtPeriodEnd { get; private set; }
    public DateTime? CanceledAt { get; private set; }
    public string? CancellationReason { get; private set; }
    
    // Metadata
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    
    // Factory methods
    public static Subscription CreateFree(Guid userId) { ... }
    public static Subscription CreateTrial(Guid userId, SubscriptionTier tier, int trialDays) { ... }
    
    // State transitions
    public void Upgrade(SubscriptionTier newTier, string stripeSubscriptionId) { ... }
    public void Downgrade(SubscriptionTier newTier) { ... }
    public void CancelAtPeriodEnd(string reason) { ... }
    public void ReactivateSubscription() { ... }
    public void RenewBillingCycle() { ... }
    public void MarkAsExpired() { ... }
}

public enum SubscriptionStatus
{
    Active = 0,
    Trialing = 1,
    PastDue = 2,
    Canceled = 3,
    Expired = 4,
    Paused = 5
}

public enum PaymentMethod
{
    None = 0,
    CreditCard = 1,
    BankTransfer = 2,
    PayPal = 3
}
```

### 2.2 Payment History (Auth Service)

```csharp
public class PaymentHistory
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    
    public PaymentStatus Status { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public string? StripeInvoiceId { get; set; }
    
    public DateTime BillingDate { get; set; }
    public DateTime? PaidAt { get; set; }
    
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }
}

public enum PaymentStatus
{
    Pending = 0,
    Succeeded = 1,
    Failed = 2,
    Refunded = 3
}
```

### 2.3 Feature Usage Tracking (Product Service)

```csharp
public class FeatureUsageSnapshot
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    
    // Current usage
    public int ActiveProductCount { get; set; }
    public int CollectionCount { get; set; }
    public int TeamMemberCount { get; set; }
    public int ApiCallsToday { get; set; }
    
    // Snapshot metadata
    public DateTime SnapshotDate { get; set; }
    public SubscriptionTier TierAtSnapshot { get; set; }
}
```

---

## 3. Quota & Limit Service

### 3.1 ISubscriptionQuotaService

```csharp
public interface ISubscriptionQuotaService
{
    Task<QuotaCheckResult> CanCreateProductAsync(Guid userId, CancellationToken ct);
    Task<QuotaCheckResult> CanCreateCollectionAsync(Guid userId, CancellationToken ct);
    Task<QuotaCheckResult> CanAddTeamMemberAsync(Guid userId, CancellationToken ct);
    Task<QuotaCheckResult> CanScheduleLaunchAsync(Guid userId, DateTime launchDate, CancellationToken ct);
    Task<int> GetRemainingAiQuotaAsync(Guid userId, CancellationToken ct);
    Task<SubscriptionTier> GetUserTierAsync(Guid userId, CancellationToken ct);
}

public record QuotaCheckResult(
    bool IsAllowed,
    string? DenialReason,
    SubscriptionTier RequiredTier,
    int CurrentUsage,
    int Limit);
```

### 3.2 Implementasyon Örneği

```csharp
public class SubscriptionQuotaService : ISubscriptionQuotaService
{
    private readonly IAuthServiceClient _authClient; // HTTP client to Auth service
    private readonly IProductRepository _productRepo;
    
    private static readonly Dictionary<SubscriptionTier, int> ProductLimits = new()
    {
        { SubscriptionTier.Free, 5 },
        { SubscriptionTier.ProMaker, int.MaxValue },
        { SubscriptionTier.Enterprise, int.MaxValue }
    };
    
    public async Task<QuotaCheckResult> CanCreateProductAsync(
        Guid userId, 
        CancellationToken ct)
    {
        // 1. Auth service'ten user tier'ını al
        var tier = await _authClient.GetSubscriptionTierAsync(userId, ct);
        
        // 2. Product service'te mevcut ürün sayısını say
        var currentCount = await _productRepo.CountActiveProductsByMakerAsync(userId, ct);
        
        // 3. Limit kontrolü
        var limit = ProductLimits[tier];
        var allowed = currentCount < limit;
        
        return new QuotaCheckResult(
            allowed,
            allowed ? null : $"You've reached the {tier} plan limit of {limit} products.",
            tier == SubscriptionTier.Free ? SubscriptionTier.ProMaker : tier,
            currentCount,
            limit);
    }
}
```

---

## 4. Payment Integration — İyzico (Türkiye)

### 4.1 Neden İyzico?

- Türk pazarına özel
- Kredi kartı, banka kartı, havale desteği
- 3D Secure entegrasyonu
- Recurring payment (otomatik ödeme) desteği
- API dokümantasyonu iyi

### 4.2 Payment Flow

```
1. Frontend: Kullanıcı "Pro'ya Geç" tıklar
   ↓
2. Backend: CreateCheckoutSession API'si
   - İyzico'da checkout session oluştur
   - Callback URL: /api/v1/payment/callback
   ↓
3. Frontend: İyzico checkout sayfasına yönlendir
   ↓
4. İyzico: Ödeme işlemi (3D Secure)
   ↓
5. Callback: İyzico webhook → /api/payment/webhook
   - Payment Success → Subscription.Upgrade()
   - Payment Failed → Log + email notification
   ↓
6. Frontend: Success sayfası + konfetti animasyonu 🎉
```

### 4.3 İyzico Service Interface

```csharp
public interface IPaymentService
{
    Task<CheckoutSession> CreateCheckoutSessionAsync(
        Guid userId, 
        SubscriptionTier tier,
        CancellationToken ct);
    
    Task<PaymentResult> HandleWebhookAsync(
        string iyzicoSignature,
        string payload,
        CancellationToken ct);
    
    Task<bool> CancelSubscriptionAsync(
        string iyzicoSubscriptionId,
        CancellationToken ct);
}

public record CheckoutSession(
    string SessionToken,
    string CheckoutUrl,
    DateTime ExpiresAt);

public record PaymentResult(
    bool Success,
    Guid SubscriptionId,
    string? ErrorMessage);
```

---

## 5. Feature Gating — Authorization Policy'leri

### 5.1 Attribute-based Gating

```csharp
[RequireSubscription(SubscriptionTier.ProMaker)]
[HttpPost("api/v1/products")]
public async Task<IResult> CreateProduct(
    [FromBody] CreateProductCommand command,
    [FromServices] ISender mediator)
{
    // Quota kontrolü command handler'da yapılır
    var result = await mediator.Send(command);
    return result.ToHttpResult();
}
```

### 5.2 Middleware-based Gating

```csharp
public class SubscriptionGatingMiddleware
{
    public async Task InvokeAsync(HttpContext context, ISubscriptionQuotaService quota)
    {
        var userId = context.User.GetUserId();
        var endpoint = context.GetEndpoint();
        
        var tierAttr = endpoint?.Metadata.GetMetadata<RequireSubscriptionAttribute>();
        if (tierAttr is null)
        {
            await _next(context);
            return;
        }
        
        var userTier = await quota.GetUserTierAsync(userId, context.RequestAborted);
        if (userTier < tierAttr.MinimumTier)
        {
            context.Response.StatusCode = 402; // Payment Required
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = 402,
                Title = "Subscription upgrade required",
                Detail = $"This feature requires {tierAttr.MinimumTier} plan or higher.",
                Extensions = { ["requiredTier"] = tierAttr.MinimumTier.ToString() }
            });
            return;
        }
        
        await _next(context);
    }
}
```

---

## 6. Migration Strategy — Mevcut Kullanıcılar

### 6.1 Grandfather Clause (Eski kullanıcıları koruma)

```sql
-- Tüm mevcut maker'lar 6 ay boyunca "grandfather" statüsünde
UPDATE Users
SET SubscriptionTier = 'Free',
    GrandfatherUntil = DATE_ADD(NOW(), INTERVAL 6 MONTH)
WHERE Role = 'Maker' AND CreatedAt < '2026-09-01';

-- CreateProduct handler'da:
if (user.GrandfatherUntil > DateTime.UtcNow)
{
    // Eski kullanıcılar için limit kontrolü atlanır
    allowedProductCount = int.MaxValue;
}
```

### 6.2 Migration Communication Plan

**Email 1 (Duyuru — 30 gün önce):**
> Vitrin'de yeni bir dönem başlıyor! 🚀
> 
> Pro Maker ve Enterprise planlarımızı tanıtıyoruz. Mevcut kullanıcılarımız için
> 6 ay boyunca tüm limitler kaldırılmış durumda. Bu sürede yeni özellikleri
> deneyebilir ve size en uygun planı seçebilirsiniz.

**Email 2 (Son 7 gün):**
> Grandfather döneminiz 7 gün sonra sona eriyor.
> 
> Şu anda 8 aktif ürününüz var. Free planında maksimum 5 ürün paylaşabilirsiniz.
> Pro Maker'a geçiş yapmak isterseniz ilk ay %50 indirimli!

---

## 7. Analytics & Reporting

### 7.1 Subscription Metrics

```csharp
public record SubscriptionMetrics
{
    public int TotalFreeUsers { get; init; }
    public int TotalProUsers { get; init; }
    public int TotalEnterpriseUsers { get; init; }
    
    public decimal MonthlyRecurringRevenue { get; init; } // MRR
    public decimal AnnualRecurringRevenue { get; init; }  // ARR
    
    public decimal ChurnRate { get; init; } // % iptal oranı
    public decimal UpgradeRate { get; init; } // Free → Pro dönüşüm
    
    public int TrialStartsThisMonth { get; init; }
    public int TrialConversionsThisMonth { get; init; }
}
```

### 7.2 Admin Dashboard Queries

```sql
-- MRR (Monthly Recurring Revenue)
SELECT 
    SUM(CASE 
        WHEN Tier = 'ProMaker' THEN 299
        WHEN Tier = 'Enterprise' THEN 999
        ELSE 0
    END) AS MRR
FROM Subscriptions
WHERE Status = 'Active';

-- Churn rate (son 30 gün)
SELECT 
    (COUNT(*) FILTER (WHERE Status = 'Canceled') * 100.0) / 
    COUNT(*) AS ChurnRate
FROM Subscriptions
WHERE CurrentPeriodEnd >= NOW() - INTERVAL '30 days';
```

---

## 8. Implementation Roadmap

### Phase 1 — Foundation (2 hafta)
- [x] Database schema (Subscription, PaymentHistory)
- [x] Auth service: Subscription aggregate + repository
- [x] ISubscriptionQuotaService interface + implementation
- [x] Migration script (mevcut kullanıcılar → Free tier)

### Phase 2 — Payment Integration (2 hafta)
- [ ] İyzico SDK integration
- [ ] Checkout session API
- [ ] Webhook handler (payment success/failure)
- [ ] Recurring payment setup
- [ ] Payment retry logic (failed payment)

### Phase 3 — Feature Gating (1 hafta)
- [ ] Product creation quota enforcement
- [ ] AI quota tier-based adjustment
- [ ] Collection limit enforcement
- [ ] API rate limit tier-based adjustment

### Phase 4 — Frontend (2 hafta)
- [ ] Pricing sayfası (/pricing)
- [ ] Checkout flow
- [ ] Subscription management sayfası (/settings/billing)
- [ ] Upgrade prompt'ları (limit aşıldığında modal)
- [ ] Badge'ler (Pro, Enterprise)

### Phase 5 — Premium Features (3 hafta)
- [ ] Gelişmiş analytics (90 gün/1 yıl)
- [ ] CSV/JSON export
- [ ] Scheduled launch (7/30 gün önceden)
- [ ] Comment pinning
- [ ] Custom URL slug
- [ ] Webhook integrations

### Phase 6 — Launch & Growth (ongoing)
- [ ] Grandfather clause activation
- [ ] Email campaign (duyuru + hatırlatmalar)
- [ ] A/B testing (pricing, messaging)
- [ ] Customer success tracking
- [ ] Churn analysis + retention campaigns

---

## 9. Security & Compliance

### 9.1 PCI-DSS Compliance
- Kredi kartı bilgileri **asla** sunucuya gelmez (İyzico hosted checkout)
- Sadece `StripeCustomerId` ve `StripeSubscriptionId` saklanır

### 9.2 KVKK (GDPR)
- Ödeme geçmişi 7 yıl saklanır (vergi mevzuatı)
- Kullanıcı hesap sildiğinde:
  - Subscription → `Status = Canceled`
  - PaymentHistory → anonimize edilir (UserId → null, email → masked)

### 9.3 Fraud Prevention
- Rate limiting: 5 checkout attempt / saat
- IP-based geolocation check
- Suspicious activity alert (aynı kart 10+ hesap)

---

## 10. Pricing Experimentation

### 10.1 İlk Fiyatlandırma (Launch)
- Pro Maker: ₺299/ay
- Enterprise: ₺999/ay
- İlk ay %50 indirim (₺149/₺499)

### 10.2 A/B Test Hipotezleri
| Test | Varyant A | Varyant B | Metrik |
|------|-----------|-----------|--------|
| Fiyat | ₺299/ay | ₺349/ay | Conversion rate |
| Trial | 14 gün | 30 gün | Trial → Paid % |
| Messaging | "Sınırsız ürün" | "Pro badge + analytics" | Click-through |

### 10.3 Beklenen Metrikler (6 ay)
- Free → Pro conversion: %5-8%
- Pro churn rate: < %10/ay
- Trial → Paid: %25-35%
- MRR growth: %15-20/ay

---

## 11. Code Structure

```
src/Services/Auth/
  ├── Domain/
  │   ├── Entities/
  │   │   ├── Subscription.cs
  │   │   └── PaymentHistory.cs
  │   └── Enums/
  │       ├── SubscriptionTier.cs
  │       └── SubscriptionStatus.cs
  ├── Application/
  │   ├── Commands/
  │   │   ├── UpgradeSubscriptionCommand.cs
  │   │   ├── CancelSubscriptionCommand.cs
  │   │   └── HandlePaymentWebhookCommand.cs
  │   ├── Queries/
  │   │   ├── GetSubscriptionQuery.cs
  │   │   └── GetSubscriptionMetricsQuery.cs
  │   └── Services/
  │       └── ISubscriptionQuotaService.cs
  └── Infrastructure/
      ├── Repositories/
      │   └── SubscriptionRepository.cs
      └── Payment/
          ├── IPaymentService.cs
          └── IyzicoPaymentService.cs

src/Shared/Infrastructure/
  └── Authorization/
      ├── RequireSubscriptionAttribute.cs
      └── SubscriptionGatingMiddleware.cs
```

---

## 12. Risks & Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| Kullanıcı tepkisi (ücretli plan) | Yüksek | Grandfather clause + 14 gün trial |
| Churn artışı | Yüksek | İlk 3 ay ücretsiz destek + onboarding |
| Payment gateway downtime | Orta | Fallback: Manuel havale + admin onay |
| Pricing çok yüksek | Yüksek | A/B test + geri besleme anketi |
| Free abuse (5 ürün limit bypass) | Düşük | Strict enforcement + audit log |

---

## 13. Success Criteria

**Launch sonrası 3 ay:**
- ✅ En az %5 Free → Pro conversion
- ✅ MRR > ₺50,000
- ✅ Churn < %15/ay
- ✅ Customer support ticket < 10/ay (subscription related)
- ✅ Sıfır payment fraud incident

**Launch sonrası 6 ay:**
- ✅ MRR > ₺150,000
- ✅ 100+ Pro Maker subscriber
- ✅ 10+ Enterprise subscriber
- ✅ NPS (Net Promoter Score) > 40

---

## Appendix A: Competitor Analysis

| Platform | Free Tier | Paid Tier | Notlar |
|----------|-----------|-----------|--------|
| Product Hunt | Sınırsız ürün | Yok | Tamamen ücretsiz |
| Indie Hackers | Sınırsız | Yok | Tamamen ücretsiz |
| BetaList | 1 ürün | $149 (one-time) | Tek seferlik ödeme |
| Launching Next | 3 ürün | $19/ay | Aylık abonelik |

**Stratejimiz:** Product Hunt'tan daha fazla özellik sunarak (analytics, AI), ücretli planı justify edebiliriz.

---

## Appendix B: Email Templates

### Upgrade Prompt Email
```
Konu: 🚀 Vitrin Pro ile ürünlerinizi bir üst seviyeye taşıyın!

Merhaba {FirstName},

Vitrin'de {ActiveProductCount} harika ürününüz var! Free planınızda maksimum 5 ürüne ulaştınız.

Pro Maker planına geçerek:
✅ Sınırsız ürün paylaşımı
✅ Gelişmiş analytics (90 gün)
✅ 50 AI analiz/gün
✅ "Pro Maker" badge

İlk ay %50 indirimli → sadece ₺149!

[Pro'ya Geç] butonu

Sevgiler,
Vitrin Ekibi
```

---

**Doküman sonu — Implementation başlayabilir! 🚀**
