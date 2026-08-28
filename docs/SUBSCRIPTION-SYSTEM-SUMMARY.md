# 💰 Vitrin Abonelik Sistemi — Özet

> **Durum:** Tasarım tamamlandı, implementasyon hazır
> **Tahmini Süre:** 6-8 hafta
> **Hedef Launch:** 15 Ekim 2026

---

## 🎯 Business Model

### Plan Karşılaştırması

| | **Free** | **Pro Maker** | **Enterprise** |
|---|---|---|---|
| **Fiyat** | ₺0 | **₺299/ay** | **₺999/ay** |
| **Ürün Sayısı** | **5 max** | **Sınırsız** | **Sınırsız** |
| **AI Analiz** | 5/gün | **50/gün** | **200/gün** |
| **Analytics Geçmişi** | 7 gün | **90 gün + CSV export** | **1 yıl + full export** |
| **Koleksiyon** | 1 | **10** | **Sınırsız** |
| **Takım Üyeleri** | Yok | **3 üye** | **10 üye** |
| **Badge** | ❌ | **🏆 Pro Maker** | **💎 Enterprise** |
| **Scheduled Launch** | ❌ | **7 gün önceden** | **30 gün önceden** |
| **API Erişimi** | ❌ | **1K istek/gün** | **10K istek/gün** |
| **Destek** | Forum | **E-posta (48 saat)** | **Öncelikli (12 saat)** |
| **Watermark** | Vitrin logo | **❌ Yok** | **❌ Yok** |

---

## 💡 Neden Premium?

### Maker'lar İçin Değer Önerileri

#### 🚀 **Pro Maker (₺299/ay)**
**Hedef kitle:** Aktif maker'lar, side project'leri ciddiye alanlar

**Temel satış argümanları:**
1. **Sınırsız ürün** — Portfolyonuzu büyütün
2. **50 AI analiz/gün** — Her ürün için otomatik öneriler
3. **90 gün analytics** — Trend'leri görün, optimize edin
4. **Pro Maker badge** — Profil güvenilirliği
5. **Öncelikli görünürlük** — Arama ve listelemelerde üstte

**Use case:** "5 ürünümü paylaştım, şimdi 6. ürünümü ekleyemiyorum" → Upgrade

---

#### 💎 **Enterprise (₺999/ay)**
**Hedef kitle:** Startup'lar, ajanslar, profesyonel ekipler

**Temel satış argümanları:**
1. **10 takım üyesi** — Ekip olarak yönetin
2. **200 AI analiz/gün** — Tüm ürünler için sürekli optimizasyon
3. **1 yıl analytics + export** — Raporlama ve yatırımcı sunumları
4. **API erişimi (10K/gün)** — Webhook, entegrasyonlar
5. **Anasayfa featured** — Maksimum görünürlük
6. **Öncelikli destek (12 saat)** — Video call dahil

**Use case:** "Ajansımız 15 müşteri adına ürün yönetiyoruz" → Enterprise

---

## 🏗️ Teknik Mimari

### Database Schema

```
Users
  └─ Subscriptions (1:1)
      ├─ Tier: Free/ProMaker/Enterprise
      ├─ Status: Active/Trialing/Canceled/Expired
      ├─ CurrentPeriodEnd: DateTime
      ├─ GrandfatherUntil: DateTime? (mevcut kullanıcılar için)
      └─ PaymentHistory (1:N)
          ├─ Amount: 299.00 TRY
          ├─ Status: Succeeded/Failed/Refunded
          └─ IyzicoPaymentId
```

### Quota Enforcement

**Product Service'te:**
```csharp
// CreateProductCommandHandler.cs
var quotaCheck = await _quotaService.CanCreateProductAsync(command.MakerId);
if (!quotaCheck.IsAllowed)
{
    return Result.Failure(
        "product.quota_exceeded",
        $"Free plan limit: {quotaCheck.Limit} products. " +
        $"Upgrade to {quotaCheck.RequiredTier} for unlimited."
    );
}
```

**AI Service'te:**
```csharp
// AiQuotaService.cs
var tier = await _quotaService.GetUserTierAsync(userId);
var dailyLimit = tier switch {
    SubscriptionTier.Free => 5,
    SubscriptionTier.ProMaker => 50,
    SubscriptionTier.Enterprise => 200
};
```

### Payment Flow (İyzico)

```
1. User clicks "Upgrade to Pro" button
   ↓
2. POST /api/v1/subscription/checkout
   → Creates Iyzico checkout session
   → Returns: checkoutUrl
   ↓
3. Redirect to Iyzico hosted page
   → User enters credit card info
   → 3D Secure verification
   ↓
4. Iyzico sends webhook: POST /api/payment/webhook
   → Verifies HMAC signature
   → Updates subscription: subscription.Upgrade(...)
   ↓
5. Redirect to /checkout/success
   → Shows confetti animation 🎉
   → "You're now a Pro Maker!"
```

---

## 🛡️ Güvenlik & Compliance

### PCI-DSS
- ✅ Kredi kartı bilgileri **sunucuya gelmez** (Iyzico hosted checkout)
- ✅ Sadece `IyzicoCustomerId` ve `IyzicoSubscriptionId` saklanır
- ✅ Webhook HMAC signature doğrulaması

### KVKK/GDPR
- ✅ Ödeme geçmişi 7 yıl saklanır (vergi mevzuatı)
- ✅ Kullanıcı silindiğinde:
  - Subscription → `Status = Canceled`
  - PaymentHistory → anonimize (`UserId → null`, `Email → masked`)

### Fraud Prevention
- ✅ Rate limiting: 5 checkout attempt/saat
- ✅ IP geolocation check
- ✅ Suspicious activity alert (aynı kart 10+ hesapta)

---

## 📈 Beklenen Metrikler

### Launch Sonrası 3 Ay

| Metrik | Hedef | Nasıl Ölçülür |
|--------|-------|---------------|
| **Free → Pro Conversion** | %5-8% | `ProUsers / TotalUsers * 100` |
| **Trial → Paid** | %25-35% | Grafana dashboard |
| **MRR (Monthly Recurring Revenue)** | ₺50,000+ | `SUM(ActiveSubs * Price)` |
| **Churn Rate** | < %15/ay | `Canceled / Total * 100` |
| **Payment Success Rate** | > %98% | `Succeeded / Total` |

### Launch Sonrası 6 Ay

| Metrik | Hedef |
|--------|-------|
| **MRR** | ₺150,000+ |
| **Pro Maker Subscribers** | 100+ |
| **Enterprise Subscribers** | 10+ |
| **NPS (Net Promoter Score)** | > 40 |

---

## 🚀 Implementation Roadmap

### Phase 1: Foundation (2 hafta)
- Database schema + migration
- Domain entities (Subscription, PaymentHistory)
- Repository pattern
- Quota service interface

### Phase 2: Payment Integration (2 hafta)
- İyzico SDK integration
- Checkout session API
- Webhook handler
- Recurring payment setup

### Phase 3: Feature Gating (1 hafta)
- Product creation quota
- AI quota tier adjustment
- Middleware & attributes

### Phase 4: Frontend (2 hafta)
- `/pricing` page
- `/settings/billing` page
- Checkout flow
- Upgrade prompt modals

### Phase 5: Premium Features (3 hafta)
- Gelişmiş analytics (90 gün/1 yıl)
- CSV/JSON export
- Scheduled launch
- Webhook integrations

### Phase 6: Launch (ongoing)
- Grandfather clause activation
- Email campaigns
- A/B testing
- Customer success tracking

---

## 🎁 Mevcut Kullanıcıları Koruma Stratejisi

### Grandfather Clause

**Ne:** Mevcut tüm maker'lar 6 ay boyunca tüm limitleri aşabilir.

**Neden:**
- Ani limit şoku yaratmamak
- Platform sadakatini korumak
- Upgrade kararı için düşünme süresi vermek

**Nasıl:**
```sql
UPDATE Users
SET GrandfatherUntil = DATE_ADD(NOW(), INTERVAL 6 MONTH)
WHERE Role = 'Maker' AND CreatedAt < '2026-09-01';
```

**Kodda:**
```csharp
if (user.Subscription.IsGrandfathered)
{
    // Tüm limitler bypass — unlimited products, collections, AI
    return QuotaCheckResult.Allow(currentUsage, int.MaxValue);
}
```

### İletişim Planı

**T-30 gün:** "Vitrin'de yeni dönem başlıyor! 🚀"
- Pro Maker ve Enterprise planları tanıtımı
- Grandfather clause duyurusu
- "6 ay boyunca tüm limitler kaldırılmış"

**T-7 gün:** "Grandfather döneminiz 7 gün sonra sona eriyor"
- Mevcut kullanım istatistikleri (örn: "8 aktif ürününüz var")
- Free plan limiti hatırlatması (5 ürün)
- İlk ay %50 indirim teklifi

**T-1 gün:** "Son 24 saat! Grandfather avantajı yarın bitiyor"
- Aciliyet hissi
- CTA: "Pro'ya Geç ve Sınırsız Ürün Paylaş"

---

## 💰 Pricing Stratejisi

### İlk Fiyatlandırma (Launch)
- **Pro Maker:** ₺299/ay
- **Enterprise:** ₺999/ay
- **İlk ay indirim:** %50 (₺149/₺499)

### A/B Test Hipotezleri

| Test | Varyant A | Varyant B | Metrik |
|------|-----------|-----------|--------|
| **Fiyat** | ₺299/ay | ₺349/ay | Conversion rate |
| **Trial süresi** | 14 gün | 30 gün | Trial → Paid % |
| **Messaging** | "Sınırsız ürün" | "Pro badge + analytics" | Click-through |
| **CTA** | "Upgrade Now" | "Start Free Trial" | Button click |

### Competitive Pricing

| Platform | Model | Fiyat | Notlar |
|----------|-------|-------|--------|
| **Vitrin (bizim)** | Freemium | ₺299/ay | 5 ürün free, Pro sınırsız |
| Product Hunt | Free | ₺0 | Tamamen ücretsiz |
| BetaList | One-time | $149 | Tek seferlik ödeme |
| Launching Next | Subscription | $19/ay (~₺550) | Aylık abonelik |

**Strateji:** Product Hunt'tan daha fazla özellik sunarak (analytics, AI, team collab), ücretli planı justify ediyoruz.

---

## 🎯 Success Criteria

### Must-Have (Launch için zorunlu)
- [x] Database schema ready
- [ ] Payment integration (Iyzico)
- [ ] Product quota enforcement (5 limit)
- [ ] Basic pricing page
- [ ] Checkout flow
- [ ] Subscription management page

### Nice-to-Have (Post-launch)
- [ ] Advanced analytics export
- [ ] Webhook integrations
- [ ] API access
- [ ] A/B testing infrastructure
- [ ] Referral program

---

## 📚 Kaynaklar

### Tasarım Dokümanları
- `docs/SUBSCRIPTION-SYSTEM-DESIGN.md` — Detaylı mimari ve business logic
- `docs/SUBSCRIPTION-IMPLEMENTATION-TASKS.md` — Task breakdown (checklist)
- `docs/SUBSCRIPTION-SYSTEM-SUMMARY.md` — Bu doküman (özet)

### Code Files (Created)
- `src/Services/Auth/Vitrin.Auth.Domain/Entities/Subscription.cs`
- `src/Services/Auth/Vitrin.Auth.Domain/Entities/PaymentHistory.cs`
- `src/Shared/Vitrin.Shared.Contracts/Subscription/ISubscriptionQuotaService.cs`

### Pricing Page Mockup
```
┌─────────────────────────────────────────────────────────┐
│                    Doğru Planı Seçin                    │
├─────────────┬────────────────────┬──────────────────────┤
│    Free     │    Pro Maker       │    Enterprise        │
│    ₺0       │    ₺299/ay         │    ₺999/ay           │
├─────────────┼────────────────────┼──────────────────────┤
│ 5 ürün      │ ✅ Sınırsız ürün   │ ✅ Sınırsız ürün     │
│ 5 AI/gün    │ ✅ 50 AI/gün       │ ✅ 200 AI/gün        │
│ 7 gün veri  │ ✅ 90 gün + export │ ✅ 1 yıl + export    │
│ ❌ Badge    │ ✅ Pro badge       │ ✅ Enterprise badge  │
│ ❌ Team     │ ✅ 3 üye           │ ✅ 10 üye            │
│ ❌ API      │ ✅ 1K/gün          │ ✅ 10K/gün + webhook │
├─────────────┼────────────────────┼──────────────────────┤
│ [Ücretsiz   │ [Pro'ya Geç]       │ [Bize Ulaşın]        │
│  Başla]     │                    │                      │
└─────────────┴────────────────────┴──────────────────────┘
```

---

## 🚦 Launch Gün Planı

### T-0 (Launch Day)

**09:00** — Feature flag açma
```bash
# Admin panel'den veya direct DB
UPDATE FeatureFlags
SET IsEnabled = true
WHERE Key = 'subscription_system_enabled';
```

**10:00** — Email kampanyası (1. dalga)
- Segment: Aktif maker'lar (son 30 günde ürün yayınlamış)
- Subject: "🚀 Vitrin Pro Maker artık burada!"

**12:00** — Social media duyurusu
- Twitter, LinkedIn, Instagram
- Blog post: "Vitrin'de Yeni Dönem"

**14:00** — Monitoring kontrol
- Grafana dashboard: Subscription Metrics
- Alert'lerin çalıştığını doğrula

**16:00** — Email kampanyası (2. dalga)
- Segment: Pasif maker'lar (90+ gün önce ürün yayınlamış)

**18:00** — Gün sonu rapor
- İlk subscription sayısı
- Checkout abandonment rate
- Payment success rate
- Support ticket sayısı

---

## 🎉 Özet

Vitrin için tam kapsamlı bir **Freemium SaaS modeli** tasarladık:

✅ **3 tier plan** (Free, Pro ₺299, Enterprise ₺999)
✅ **Quota sistemi** (5 ürün limiti, tier-based AI kotası)
✅ **İyzico payment integration** (Türkiye'ye özel)
✅ **Grandfather clause** (mevcut kullanıcıları koruma)
✅ **Feature gating** (middleware + attributes)
✅ **Premium features** (analytics export, team collab, API access)
✅ **6-8 haftalık implementasyon roadmap**

**Hedef:** 3 ay içinde ₺50K+ MRR, 6 ay içinde ₺150K+ MRR

**Risk mitigasyonu:** Grandfather clause + 14 gün trial + A/B testing

**Ready to implement!** 🚀
