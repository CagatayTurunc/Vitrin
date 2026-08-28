# Subscription System Implementation Tasks

> Oluşturulma: 27 Ağustos 2026
> Durum: Planning → Implementation
> Tahmini süre: 6-8 hafta

---

## 📋 Task Breakdown

### Phase 1: Domain & Infrastructure (Hafta 1-2)

#### ✅ Completed
- [x] Domain entities: `Subscription`, `PaymentHistory`
- [x] Shared contracts: `ISubscriptionQuotaService`
- [x] Enum definitions: `SubscriptionTier`, `SubscriptionStatus`, `PaymentMethod`

#### 🔄 In Progress
- [ ] **Auth Service — Database Schema**
  - [ ] EF Core DbContext güncelleme (`AuthDbContext`)
  - [ ] Migration: `AddSubscriptionSystem`
  - [ ] Seed data: Tüm mevcut User'lar → Free tier
  - [ ] Index: `IX_Subscriptions_UserId`, `IX_Subscriptions_Status`
  - [ ] Index: `IX_PaymentHistory_SubscriptionId`, `IX_PaymentHistory_UserId`

- [ ] **Repository Pattern**
  - [ ] `ISubscriptionRepository` interface
  - [ ] `SubscriptionRepository` implementation
  - [ ] `IPaymentHistoryRepository` interface
  - [ ] `PaymentHistoryRepository` implementation

- [ ] **User Entity Güncelleme**
  ```csharp
  // User.cs'e ekle:
  public Guid? SubscriptionId { get; private set; }
  public Subscription? Subscription { get; private set; }
  
  public void AttachSubscription(Subscription subscription)
  {
      SubscriptionId = subscription.Id;
      Subscription = subscription;
  }
  ```

---

### Phase 2: Application Layer (Hafta 2-3)

#### Commands (Write Operations)
- [ ] **UpgradeSubscriptionCommand**
  - Input: `UserId`, `TargetTier`, `PaymentMethodId`
  - Output: `CheckoutSessionUrl`
  - Handler: Creates Iyzico checkout session

- [ ] **CancelSubscriptionCommand**
  - Input: `UserId`, `Reason`, `CancelImmediately`
  - Output: Success/Failure
  - Handler: Marks subscription for cancellation

- [ ] **ReactivateSubscriptionCommand**
  - Input: `UserId`
  - Output: Success/Failure
  - Handler: Removes cancellation flag

- [ ] **HandlePaymentWebhookCommand**
  - Input: `IyzicoPayload`, `Signature`
  - Output: Processed/Ignored
  - Handler: Validates webhook, updates subscription

#### Queries (Read Operations)
- [ ] **GetSubscriptionQuery**
  - Input: `UserId`
  - Output: `SubscriptionDto`

- [ ] **GetPaymentHistoryQuery**
  - Input: `UserId`, `PageSize`, `Cursor`
  - Output: Paginated payment list

- [ ] **GetSubscriptionMetricsQuery** (Admin only)
  - Input: None
  - Output: MRR, churn rate, active subs

#### Services
- [ ] **SubscriptionQuotaService** (implements `ISubscriptionQuotaService`)
  - `CanCreateProductAsync()` — Auth'tan tier al + Product'tan count al
  - `CanCreateCollectionAsync()`
  - `GetUserTierAsync()` — Cache ile (Redis, 15 dakika TTL)

- [ ] **IPaymentService Interface**
  ```csharp
  public interface IPaymentService
  {
      Task<CheckoutSession> CreateCheckoutSessionAsync(...);
      Task<PaymentWebhookResult> HandleWebhookAsync(...);
      Task<bool> CancelRecurringPaymentAsync(...);
      Task<RefundResult> RefundPaymentAsync(...);
  }
  ```

- [ ] **IyzicoPaymentService** (implementation)
  - SDK: `iyzipay-dotnet` NuGet package
  - Checkout session creation
  - Webhook signature verification
  - Recurring payment setup

---

### Phase 3: API Endpoints (Hafta 3)

#### Public Endpoints (`/api/v1/subscription`)
- [ ] `POST /checkout` — Start upgrade flow
- [ ] `GET /current` — Get user's subscription info
- [ ] `POST /cancel` — Cancel subscription
- [ ] `POST /reactivate` — Undo cancellation
- [ ] `GET /payment-history` — List past payments

#### Webhook Endpoint
- [ ] `POST /api/payment/webhook` — Iyzico callback
  - HMAC signature validation
  - Idempotency check (duplicate webhook prevention)
  - Event types: `payment.succeeded`, `payment.failed`, `subscription.renewed`

#### Admin Endpoints (`/api/v1/admin/subscription`)
- [ ] `GET /metrics` — Dashboard metrics (MRR, churn, etc.)
- [ ] `GET /users` — List subscribers with filters
- [ ] `POST /users/{userId}/override-tier` — Manual tier override (refund case)

---

### Phase 4: Feature Gating (Hafta 4)

#### Middleware
- [ ] **SubscriptionGatingMiddleware**
  ```csharp
  app.UseAuthentication();
  app.UseSubscriptionGating(); // ← New middleware
  app.UseAuthorization();
  ```

#### Authorization Attributes
- [ ] **RequireSubscriptionAttribute**
  ```csharp
  [RequireSubscription(SubscriptionTier.ProMaker)]
  [HttpPost("api/v1/products")]
  public async Task<IResult> CreateProduct(...)
  ```

#### Product Service Integration
- [ ] **CreateProductCommandHandler güncelleme**
  ```csharp
  public async Task<Result<ProductResponse>> Handle(...)
  {
      // Quota check
      var quotaCheck = await _quotaService.CanCreateProductAsync(command.MakerId);
      if (!quotaCheck.IsAllowed)
          return Result<ProductResponse>.Failure(
              "product.quota_exceeded",
              quotaCheck.DenialReason!);
      
      // ... rest of logic
  }
  ```

- [ ] **CreateCollectionCommandHandler güncelleme**
- [ ] **ScheduleLaunchCommandHandler güncelleme**

#### AI Service Integration
- [ ] **AiQuotaService güncellemesi**
  ```csharp
  // Mevcut: Herkese 10/gün
  // Yeni: Tier-based limit
  var tier = await _quotaService.GetUserTierAsync(userId);
  var dailyLimit = tier switch {
      SubscriptionTier.Free => 5,
      SubscriptionTier.ProMaker => 50,
      SubscriptionTier.Enterprise => 200,
      _ => 5
  };
  ```

---

### Phase 5: Frontend (Hafta 5-6)

#### Yeni Sayfalar
- [ ] **`/pricing`** — Plan karşılaştırma sayfası
  - 3 sütun: Free, Pro, Enterprise
  - Feature comparison table
  - CTA butonları: "Get Started" / "Upgrade Now"
  - FAQ section

- [ ] **`/settings/billing`** — Subscription management
  - Current plan badge
  - Usage statistics (product count, AI quota)
  - Payment history table
  - Upgrade/Downgrade buttons
  - Cancel subscription (with confirmation modal)

- [ ] **`/checkout`** — Payment flow
  - Plan seçimi recap
  - Iyzico embedded form
  - Loading state
  - Success/Error handling

#### Upgrade Prompt Modals
- [ ] **ProductQuotaExceededModal**
  - Trigger: User tries to create 6th product on Free
  - Content: "You've reached the Free plan limit (5 products)"
  - CTA: "Upgrade to Pro Maker"

- [ ] **AIQuotaExceededModal**
  - Trigger: Daily AI quota depleted
  - Content: "You've used all 5 AI analyses today"
  - CTA: "Upgrade for 50/day"

#### UI Components
- [ ] **SubscriptionBadge** component
  ```tsx
  <SubscriptionBadge tier="ProMaker" />
  // Renders: 🏆 Pro Maker
  ```

- [ ] **PlanComparisonTable** component
- [ ] **PaymentHistoryTable** component
- [ ] **UsageProgressBar** component
  ```tsx
  <UsageProgressBar 
    current={3} 
    limit={5} 
    label="Products" 
  />
  ```

#### Navigation Updates
- [ ] Header'a "Upgrade" butonu (Free users için)
- [ ] Settings menüsüne "Billing" tab'ı
- [ ] User menu'de plan badge gösterimi

---

### Phase 6: Background Jobs (Hafta 6)

- [ ] **SubscriptionExpirationWorker**
  - Zamanlama: Her gün 02:00 UTC
  - Görev: `CurrentPeriodEnd` geçen ve ödeme yapılmayan subscription'ları expire et
  - Logic: `subscription.MarkAsExpired()` → Downgrade to Free

- [ ] **PaymentRetryWorker**
  - Zamanlama: Her gün 03:00 UTC
  - Görev: PastDue subscription'lar için payment retry
  - Retry schedule: 3, 7, 14 gün sonra
  - Max retry: 3 deneme, sonra expire

- [ ] **SubscriptionRenewalReminderWorker**
  - Zamanlama: Her gün 10:00 UTC
  - Görev: 3 gün içinde expire olacak subscription'lara email gönder
  - Template: "Your Pro Maker plan expires in 3 days"

---

### Phase 7: Testing (Hafta 7)

#### Unit Tests
- [ ] `Subscription` domain entity tests
  - `CreateFree()`, `CreateTrial()`, `CreateGrandfathered()`
  - `Upgrade()`, `Downgrade()`, `CancelAtPeriodEnd()`
  - `RenewBillingCycle()`, `MarkAsExpired()`

- [ ] `PaymentHistory` entity tests
  - `MarkAsSucceeded()`, `MarkAsFailed()`
  - `IncrementRetry()`, `MarkAsRefunded()`

- [ ] `SubscriptionQuotaService` tests
  - Mock Auth service client
  - Test all quota checks (product, collection, team)

#### Integration Tests
- [ ] Subscription lifecycle end-to-end
  - Free → Trial → Pro (with payment)
  - Pro → Cancel → Expire → Free
  - Grandfather clause bypass

- [ ] Payment webhook handling
  - Success case
  - Failure case
  - Duplicate webhook (idempotency)

- [ ] Quota enforcement
  - Free user creates 5 products → 6th rejected
  - After upgrade to Pro → unlimited

#### E2E Tests (Playwright)
- [ ] Upgrade flow: Free → Pro
  - Navigate to /pricing
  - Click "Upgrade to Pro"
  - Fill payment form (Iyzico test card)
  - Verify success page
  - Verify badge shows "Pro Maker"

- [ ] Cancellation flow
  - Navigate to /settings/billing
  - Click "Cancel Subscription"
  - Confirm modal
  - Verify "Cancels on [date]" message

---

### Phase 8: Deployment & Launch (Hafta 8)

#### Pre-Launch Checklist
- [ ] Environment variables
  ```bash
  IYZICO_API_KEY=sandbox_...
  IYZICO_SECRET_KEY=sandbox_...
  IYZICO_BASE_URL=https://sandbox-api.iyzipay.com
  SUBSCRIPTION_WEBHOOK_SECRET=...
  ```

- [ ] Database migration (production)
  ```bash
  # Backup önce!
  docker exec vitrin-postgres pg_dump -U postgres vitrin_auth > backup_pre_subscription.sql
  
  # Migration
  dotnet ef database update --project Auth.Api
  ```

- [ ] Grandfather clause activation
  ```sql
  UPDATE Users
  SET GrandfatherUntil = DATE_ADD(NOW(), INTERVAL 6 MONTH)
  WHERE Role = 'Maker' AND CreatedAt < '2026-09-01';
  ```

#### Launch Communication
- [ ] **Email 1 — Duyuru (T-30 gün)**
  - Subject: "🚀 Vitrin'de Yeni Dönem: Pro Maker Planı!"
  - Content: Plan özellikleri, fiyatlandırma, grandfather clause
  - CTA: "Hemen İncele"

- [ ] **Email 2 — Hatırlatma (T-7 gün)**
  - Subject: "7 gün kaldı! Grandfather avantajınızdan yararlanın"
  - Content: Free plan limitleri, Pro avantajları, ilk ay %50 indirim
  - CTA: "Pro'ya Geç"

- [ ] **Email 3 — Son Çağrı (T-1 gün)**
  - Subject: "Son 24 saat! Grandfather dönemi yarın bitiyor"
  - Personalized: "Şu anda {ProductCount} ürününüz var"

#### Monitoring & Alerts
- [ ] Grafana dashboard: Subscription Metrics
  - MRR trend
  - Active subscriptions (by tier)
  - Churn rate
  - Trial → Paid conversion

- [ ] Alerting rules
  - Payment webhook failure rate > %5
  - Churn rate > %15/ay
  - Iyzico API downtime

#### Rollback Plan
- [ ] Feature flag: `subscription_system_enabled`
  - Default: `false`
  - Launch day: `true`
  - If emergency: `false` (revert to unlimited free)

---

## 🎯 Definition of Done

Her task için DoD:
- [ ] Code yazıldı ve review edildi
- [ ] Unit test coverage > %80
- [ ] Integration test yazıldı
- [ ] API endpoint Swagger'da dokumentlendi
- [ ] Frontend component Storybook'ta görselleştirildi
- [ ] Error handling eklendi (ProblemDetails)
- [ ] Logging eklendi (Serilog structured log)
- [ ] Metrics eklendi (Prometheus counter/histogram)

---

## 📊 Success Metrics (Launch sonrası 3 ay)

| Metrik | Hedef | Nasıl Ölçülür |
|--------|-------|---------------|
| Free → Pro conversion | %5-8% | Grafana dashboard |
| Trial → Paid conversion | %25-35% | `TrialStarted` vs `TrialConverted` event |
| MRR | ₺50,000+ | Sum of active Pro/Enterprise subs |
| Churn rate | < %15/ay | `(Canceled subs / Total subs) * 100` |
| Payment success rate | > %98 | `Succeeded / (Succeeded + Failed)` |
| Customer support tickets | < 10/ay | Zendesk/Intercom |

---

## 🚀 Next Steps

1. **Hafta 1-2:** Domain entities + Database migration
2. **Hafta 2-3:** Application layer + Iyzico integration
3. **Hafta 3-4:** API endpoints + Feature gating
4. **Hafta 5-6:** Frontend (pricing page + billing settings)
5. **Hafta 7:** Testing + Bug fixing
6. **Hafta 8:** Launch preparation + Communication

**Başlangıç tarihi:** 28 Ağustos 2026
**Hedef launch:** 15 Ekim 2026

---

**Task tracking:** Bu dosya `docs/` altında tutulur, her task tamamlandıkça checkbox işaretlenir.
