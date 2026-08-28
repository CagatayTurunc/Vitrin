# 💳 İyzico Payment Integration — Quick Start Guide

> **Hedef:** 2-3 günde çalışan bir ödeme sistemi
> **Status:** Step-by-step implementation guide

---

## 📋 Checklist

### Phase 1: İyzico Setup (15 dakika)
- [ ] İyzico merchant hesabı aç
- [ ] Sandbox credentials al
- [ ] Environment variables ekle
- [ ] Test kartlarını not al

### Phase 2: Backend Implementation (1 gün)
- [ ] NuGet package ekle
- [ ] Payment service interface oluştur
- [ ] IyzicoPaymentService implement et
- [ ] Checkout endpoint ekle
- [ ] Callback handler ekle
- [ ] Subscription commands oluştur

### Phase 3: Database & Domain (0.5 gün)
- [ ] Migration: AddSubscriptionSystem
- [ ] Seed: Mevcut kullanıcılar → Free tier

### Phase 4: Frontend (0.5 gün)
- [ ] Pricing page temel layout
- [ ] Checkout button
- [ ] Success/Error pages

### Phase 5: Testing (0.5 gün)
- [ ] Test kartı ile checkout
- [ ] Callback testi
- [ ] Subscription upgrade doğrulama

---

## 🎯 Step 1: İyzico Hesabı Oluştur

### 1.1 Kayıt
```
1. https://merchant.iyzico.com/auth/register
2. Form doldur (şirket bilgileri)
3. Email doğrulama
4. Sandbox dashboard'a eriş
```

### 1.2 API Credentials
```
Dashboard → Ayarlar → API Anahtarları

Sandbox:
- API Key: sandbox_xxxxxxxxx
- Secret Key: sandbox_yyyyyyyyy
- Base URL: https://sandbox-api.iyzipay.com

Production (sonra):
- API Key: live_xxxxxxxxx
- Secret Key: live_yyyyyyyyy
- Base URL: https://api.iyzipay.com
```

### 1.3 Environment Variables
```bash
# Vitrin/.env dosyasına ekle:

# İyzico Payment Gateway
IYZICO_API_KEY=sandbox_xxxxxxxxx
IYZICO_SECRET_KEY=sandbox_yyyyyyyyy
IYZICO_BASE_URL=https://sandbox-api.iyzipay.com
PAYMENT_WEBHOOK_SECRET=generate_random_32_char_string_here
```

### 1.4 Test Kartları
```
Başarılı ödeme:
  Kart No: 5528790000000008
  Ay/Yıl: 12/30
  CVC: 123

Başarısız ödeme:
  Kart No: 5406670000000009
  Ay/Yıl: 12/30
  CVC: 123

3D Secure test şifresi: 123456
```

---

## 🔧 Step 2: Backend Implementation

### 2.1 NuGet Package Ekle
```bash
cd src/Services/Auth/Vitrin.Auth.Api
dotnet add package Iyzipay --version 2.1.40
```

### 2.2 Payment Service Interface
```csharp
// src/Services/Auth/Vitrin.Auth.Application/Services/IPaymentService.cs

namespace Vitrin.Auth.Application.Services;

public interface IPaymentService
{
    /// <summary>
    /// Creates an Iyzico checkout session for subscription upgrade.
    /// Returns hosted payment page URL.
    /// </summary>
    Task<string> CreateCheckoutSessionAsync(
        Guid userId,
        string email,
        string fullName,
        SubscriptionTier targetTier,
        CancellationToken ct = default);

    /// <summary>
    /// Validates Iyzico callback token and retrieves payment result.
    /// </summary>
    Task<PaymentResult> RetrievePaymentResultAsync(
        string token,
        CancellationToken ct = default);

    /// <summary>
    /// Validates webhook HMAC signature.
    /// </summary>
    bool ValidateWebhookSignature(string signature, string payload);

    /// <summary>
    /// Cancels recurring subscription in Iyzico.
    /// </summary>
    Task<bool> CancelRecurringSubscriptionAsync(
        string iyzicoSubscriptionId,
        CancellationToken ct = default);
}

public record PaymentResult(
    bool Success,
    string? PaymentId,
    string? ConversationId,
    decimal? PaidAmount,
    string? ErrorMessage);
```

### 2.3 İyzico Implementation
```csharp
// src/Services/Auth/Vitrin.Auth.Infrastructure/Payment/IyzicoPaymentService.cs

using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vitrin.Auth.Application.Services;

namespace Vitrin.Auth.Infrastructure.Payment;

public sealed class IyzicoPaymentService : IPaymentService
{
    private readonly Options _options;
    private readonly ILogger<IyzicoPaymentService> _logger;
    private readonly string _callbackBaseUrl;

    public IyzicoPaymentService(
        IConfiguration config,
        ILogger<IyzicoPaymentService> logger)
    {
        _options = new Options
        {
            ApiKey = config["Iyzico:ApiKey"] 
                ?? throw new InvalidOperationException("Iyzico:ApiKey is required"),
            SecretKey = config["Iyzico:SecretKey"] 
                ?? throw new InvalidOperationException("Iyzico:SecretKey is required"),
            BaseUrl = config["Iyzico:BaseUrl"] 
                ?? "https://sandbox-api.iyzipay.com"
        };
        
        _logger = logger;
        _callbackBaseUrl = config["App:BaseUrl"] ?? "https://vitrin.it.com";
    }

    public async Task<string> CreateCheckoutSessionAsync(
        Guid userId,
        string email,
        string fullName,
        SubscriptionTier targetTier,
        CancellationToken ct = default)
    {
        var (price, paidPrice) = GetPriceForTier(targetTier);
        var conversationId = Guid.NewGuid().ToString();

        var request = new CreateCheckoutFormInitializeRequest
        {
            Locale = Locale.TR.ToString(),
            ConversationId = conversationId,
            Price = price,
            PaidPrice = paidPrice,
            Currency = Currency.TRY.ToString(),
            BasketId = userId.ToString(),
            PaymentGroup = PaymentGroup.SUBSCRIPTION.ToString(),
            
            CallbackUrl = $"{_callbackBaseUrl}/api/payment/callback",
            
            Buyer = new Buyer
            {
                Id = userId.ToString(),
                Name = fullName.Split(' ').FirstOrDefault() ?? "User",
                Surname = fullName.Split(' ').Skip(1).FirstOrDefault() ?? "User",
                Email = email,
                IdentityNumber = "11111111111", // Test için — production'da gerçek TC
                RegistrationAddress = "Türkiye",
                City = "İstanbul",
                Country = "Turkey",
                Ip = "85.34.78.112" // Frontend'ten alınacak
            },
            
            BasketItems = new List<BasketItem>
            {
                new()
                {
                    Id = targetTier.ToString(),
                    Name = $"Vitrin {targetTier} Membership",
                    Category1 = "Subscription",
                    ItemType = BasketItemType.VIRTUAL.ToString(),
                    Price = price
                }
            },
            
            EnabledInstallments = new List<int> { 1 } // Tek çekim
        };

        var response = await Task.Run(() => 
            CheckoutFormInitialize.Create(request, _options), ct);

        if (response.Status == "success")
        {
            _logger.LogInformation(
                "Checkout session created: ConversationId={ConversationId}, UserId={UserId}, Tier={Tier}",
                conversationId, userId, targetTier);
            
            return response.PaymentPageUrl;
        }

        _logger.LogError(
            "Iyzico checkout failed: {ErrorCode} - {ErrorMessage}",
            response.ErrorCode, response.ErrorMessage);
        
        throw new InvalidOperationException(
            $"Payment gateway error: {response.ErrorMessage}");
    }

    public async Task<PaymentResult> RetrievePaymentResultAsync(
        string token,
        CancellationToken ct = default)
    {
        var request = new RetrieveCheckoutFormRequest { Token = token };
        
        var response = await Task.Run(() => 
            CheckoutForm.Retrieve(request, _options), ct);

        if (response.Status == "success" && response.PaymentStatus == "SUCCESS")
        {
            return new PaymentResult(
                Success: true,
                PaymentId: response.PaymentId,
                ConversationId: response.ConversationId,
                PaidAmount: decimal.Parse(response.PaidPrice),
                ErrorMessage: null);
        }

        return new PaymentResult(
            Success: false,
            PaymentId: null,
            ConversationId: response.ConversationId,
            PaidAmount: null,
            ErrorMessage: response.ErrorMessage);
    }

    public bool ValidateWebhookSignature(string signature, string payload)
    {
        // İyzico webhook HMAC validation
        // TODO: Implement HMAC-SHA256 validation
        return true;
    }

    public async Task<bool> CancelRecurringSubscriptionAsync(
        string iyzicoSubscriptionId,
        CancellationToken ct = default)
    {
        // İyzico subscription cancel API
        // TODO: Implement when recurring payment is needed
        await Task.CompletedTask;
        return true;
    }

    private static (string price, string paidPrice) GetPriceForTier(SubscriptionTier tier)
    {
        return tier switch
        {
            SubscriptionTier.ProMaker => ("299.00", "299.00"),
            SubscriptionTier.Enterprise => ("999.00", "999.00"),
            _ => throw new ArgumentException($"Invalid tier for payment: {tier}")
        };
    }
}
```

### 2.4 Register Service
```csharp
// src/Services/Auth/Vitrin.Auth.Api/Program.cs

// Payment service registration
builder.Services.AddSingleton<IPaymentService, IyzicoPaymentService>();
```

---

## 📡 Step 3: API Endpoints

### 3.1 Checkout Endpoint
```csharp
// src/Services/Auth/Vitrin.Auth.Api/Endpoints/SubscriptionEndpoints.cs

public static class SubscriptionEndpoints
{
    public static void MapSubscriptionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("api/v1/subscription")
            .WithTags("Subscription");

        // Start checkout
        group.MapPost("checkout", StartCheckout)
            .RequireAuthorization()
            .WithName("StartCheckout");

        // Payment callback (Iyzico redirects here)
        group.MapPost("callback", HandleCallback)
            .AllowAnonymous()
            .WithName("PaymentCallback");

        // Get current subscription
        group.MapGet("current", GetCurrentSubscription)
            .RequireAuthorization()
            .WithName("GetSubscription");
    }

    private static async Task<IResult> StartCheckout(
        [FromBody] StartCheckoutRequest request,
        HttpContext context,
        IPaymentService paymentService,
        IUserRepository userRepo,
        CancellationToken ct)
    {
        var userId = context.User.GetUserId();
        var user = await userRepo.GetByIdAsync(userId, ct);
        
        if (user is null)
            return Results.NotFound();

        // Validate tier upgrade
        if (request.TargetTier <= user.Subscription?.Tier)
        {
            return Results.Problem(
                title: "Invalid upgrade",
                detail: $"Cannot upgrade from {user.Subscription?.Tier} to {request.TargetTier}.",
                statusCode: 400);
        }

        try
        {
            var checkoutUrl = await paymentService.CreateCheckoutSessionAsync(
                userId,
                user.Email,
                user.FullName,
                request.TargetTier,
                ct);

            return Results.Ok(new { checkoutUrl });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: "Payment gateway error",
                detail: ex.Message,
                statusCode: 502);
        }
    }

    private static async Task<IResult> HandleCallback(
        [FromForm] string token,
        IPaymentService paymentService,
        ISender mediator,
        CancellationToken ct)
    {
        var paymentResult = await paymentService.RetrievePaymentResultAsync(token, ct);

        if (paymentResult.Success)
        {
            // Complete subscription upgrade
            var command = new CompleteSubscriptionUpgradeCommand(
                ConversationId: paymentResult.ConversationId!,
                PaymentId: paymentResult.PaymentId!,
                PaidAmount: paymentResult.PaidAmount!.Value);

            var result = await mediator.Send(command, ct);

            if (result.IsSuccess)
            {
                return Results.Redirect("/checkout/success");
            }
        }

        return Results.Redirect($"/checkout/failed?reason={paymentResult.ErrorMessage}");
    }

    private static async Task<IResult> GetCurrentSubscription(
        HttpContext context,
        ISubscriptionRepository repo,
        CancellationToken ct)
    {
        var userId = context.User.GetUserId();
        var subscription = await repo.GetByUserIdAsync(userId, ct);

        if (subscription is null)
            return Results.NotFound();

        return Results.Ok(new SubscriptionResponse(
            subscription.Tier,
            subscription.Status,
            subscription.CurrentPeriodEnd,
            subscription.CancelAtPeriodEnd));
    }
}

public record StartCheckoutRequest(SubscriptionTier TargetTier);

public record SubscriptionResponse(
    SubscriptionTier Tier,
    SubscriptionStatus Status,
    DateTime CurrentPeriodEnd,
    bool CancelAtPeriodEnd);
```

### 3.2 Register Endpoints
```csharp
// Program.cs'e ekle
app.MapSubscriptionEndpoints();
```

---

## 🗄️ Step 4: Commands

### 4.1 Complete Upgrade Command
```csharp
// src/Services/Auth/Vitrin.Auth.Application/Commands/CompleteSubscriptionUpgradeCommand.cs

public record CompleteSubscriptionUpgradeCommand(
    string ConversationId,
    string PaymentId,
    decimal PaidAmount) : IRequest<Result>;

public sealed class CompleteSubscriptionUpgradeCommandHandler 
    : IRequestHandler<CompleteSubscriptionUpgradeCommand, Result>
{
    private readonly ISubscriptionRepository _repo;
    private readonly IPaymentHistoryRepository _paymentRepo;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result> Handle(
        CompleteSubscriptionUpgradeCommand command,
        CancellationToken ct)
    {
        // 1. Find subscription by conversation ID (stored in payment history)
        var payment = await _paymentRepo.GetByConversationIdAsync(command.ConversationId, ct);
        if (payment is null)
            return Result.Failure("payment.not_found", "Payment record not found.");

        var subscription = await _repo.GetByIdAsync(payment.SubscriptionId, ct);
        if (subscription is null)
            return Result.Failure("subscription.not_found", "Subscription not found.");

        // 2. Mark payment as succeeded
        payment.MarkAsSucceeded(command.PaymentId, command.ConversationId);

        // 3. Upgrade subscription
        var tier = command.PaidAmount == 299m 
            ? SubscriptionTier.ProMaker 
            : SubscriptionTier.Enterprise;

        subscription.Upgrade(
            tier,
            iyzicoCustomerId: "iyzico_customer_" + subscription.UserId,
            iyzicoSubscriptionId: command.PaymentId,
            PaymentMethod.CreditCard);

        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
```

---

## 🎨 Step 5: Frontend Implementation

### 5.1 Pricing Page
```tsx
// src/Web/Vitrin.Web.UI/app/pricing/page.tsx

export default function PricingPage() {
  const { data: session } = useSession();
  const [loading, setLoading] = useState<SubscriptionTier | null>(null);

  const handleUpgrade = async (tier: SubscriptionTier) => {
    if (!session) {
      router.push('/login?redirect=/pricing');
      return;
    }

    setLoading(tier);

    try {
      const response = await fetch('/api/v1/subscription/checkout', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${session.accessToken}`
        },
        body: JSON.stringify({ targetTier: tier })
      });

      if (!response.ok) {
        const error = await response.json();
        toast.error(error.detail || 'Checkout failed');
        return;
      }

      const { checkoutUrl } = await response.json();
      window.location.href = checkoutUrl; // İyzico'ya yönlendir
    } catch (error) {
      toast.error('An error occurred');
    } finally {
      setLoading(null);
    }
  };

  return (
    <div className="container mx-auto py-12">
      <h1 className="text-4xl font-bold text-center mb-12">
        Doğru Planı Seçin
      </h1>

      <div className="grid md:grid-cols-3 gap-8 max-w-6xl mx-auto">
        {/* Free Plan */}
        <PricingCard
          name="Free"
          price="₺0"
          period="/ay"
          features={[
            '5 ürün',
            '5 AI analiz/gün',
            '7 gün analytics',
            'Community destek'
          ]}
          cta="Mevcut Plan"
          disabled
        />

        {/* Pro Maker */}
        <PricingCard
          name="Pro Maker"
          price="₺299"
          period="/ay"
          badge="En Popüler"
          features={[
            'Sınırsız ürün',
            '50 AI analiz/gün',
            '90 gün analytics + export',
            '🏆 Pro Maker badge',
            'Öncelikli sıralama',
            'Email destek (48 saat)'
          ]}
          cta="Pro'ya Geç"
          onUpgrade={() => handleUpgrade('ProMaker')}
          loading={loading === 'ProMaker'}
          highlighted
        />

        {/* Enterprise */}
        <PricingCard
          name="Enterprise"
          price="₺999"
          period="/ay"
          features={[
            'Pro\'nun tüm özellikleri',
            '200 AI analiz/gün',
            '1 yıl analytics',
            '10 takım üyesi',
            'API access (10K/gün)',
            'Öncelikli destek (12 saat)'
          ]}
          cta="Bize Ulaşın"
          onUpgrade={() => handleUpgrade('Enterprise')}
          loading={loading === 'Enterprise'}
        />
      </div>
    </div>
  );
}
```

### 5.2 Pricing Card Component
```tsx
// components/PricingCard.tsx

interface PricingCardProps {
  name: string;
  price: string;
  period: string;
  badge?: string;
  features: string[];
  cta: string;
  onUpgrade?: () => void;
  loading?: boolean;
  disabled?: boolean;
  highlighted?: boolean;
}

export function PricingCard({
  name,
  price,
  period,
  badge,
  features,
  cta,
  onUpgrade,
  loading,
  disabled,
  highlighted
}: PricingCardProps) {
  return (
    <div className={cn(
      "relative rounded-2xl p-8 border-2",
      highlighted 
        ? "border-primary shadow-2xl scale-105" 
        : "border-gray-200"
    )}>
      {badge && (
        <div className="absolute -top-4 left-1/2 -translate-x-1/2">
          <span className="bg-primary text-white px-4 py-1 rounded-full text-sm font-medium">
            {badge}
          </span>
        </div>
      )}

      <div className="text-center mb-6">
        <h3 className="text-2xl font-bold mb-2">{name}</h3>
        <div className="flex items-baseline justify-center">
          <span className="text-5xl font-bold">{price}</span>
          <span className="text-gray-500 ml-1">{period}</span>
        </div>
      </div>

      <ul className="space-y-3 mb-8">
        {features.map((feature, i) => (
          <li key={i} className="flex items-start">
            <Check className="w-5 h-5 text-green-500 mr-2 flex-shrink-0 mt-0.5" />
            <span className="text-sm">{feature}</span>
          </li>
        ))}
      </ul>

      <Button
        className="w-full"
        variant={highlighted ? 'default' : 'outline'}
        onClick={onUpgrade}
        disabled={disabled || loading}
      >
        {loading ? (
          <>
            <Loader2 className="w-4 h-4 mr-2 animate-spin" />
            İşleniyor...
          </>
        ) : (
          cta
        )}
      </Button>
    </div>
  );
}
```

### 5.3 Success Page
```tsx
// app/checkout/success/page.tsx

export default function CheckoutSuccessPage() {
  useEffect(() => {
    confetti({
      particleCount: 100,
      spread: 70,
      origin: { y: 0.6 }
    });
  }, []);

  return (
    <div className="container mx-auto py-20 text-center">
      <div className="max-w-md mx-auto">
        <div className="mb-6">
          <div className="w-20 h-20 bg-green-100 rounded-full flex items-center justify-center mx-auto">
            <Check className="w-10 h-10 text-green-600" />
          </div>
        </div>

        <h1 className="text-3xl font-bold mb-4">
          Hoş geldiniz, Pro Maker! 🎉
        </h1>

        <p className="text-gray-600 mb-8">
          Aboneliğiniz başarıyla aktif edildi. Artık sınırsız ürün paylaşabilir,
          gelişmiş analytics kullanabilir ve Pro badge'inizi gösterebilirsiniz!
        </p>

        <div className="space-y-3">
          <Link href="/dashboard">
            <Button className="w-full">Dashboard'a Git</Button>
          </Link>
          
          <Link href="/products/new">
            <Button variant="outline" className="w-full">
              İlk Ürünümü Ekle
            </Button>
          </Link>
        </div>
      </div>
    </div>
  );
}
```

---

## ✅ Testing Checklist

### Manual Testing
```bash
1. [ ] http://localhost:3003/pricing sayfasını aç
2. [ ] "Pro'ya Geç" butonuna tıkla
3. [ ] İyzico checkout sayfasına yönlendirildiğini doğrula
4. [ ] Test kartı ile ödeme yap:
       Kart: 5528790000000008
       Tarih: 12/30
       CVC: 123
       3D Secure: 123456
5. [ ] Success sayfasına yönlendirildiğini doğrula
6. [ ] Confetti animasyonunu gör 🎉
7. [ ] Database'de subscription güncellendi mi kontrol et
```

### Database Verification
```sql
-- User'ın subscription'ını kontrol et
SELECT u.Email, s.Tier, s.Status, s.CurrentPeriodEnd
FROM Users u
JOIN Subscriptions s ON s.UserId = u.Id
WHERE u.Email = 'test@example.com';

-- Payment history'yi kontrol et
SELECT * FROM PaymentHistory
WHERE UserId = (SELECT Id FROM Users WHERE Email = 'test@example.com')
ORDER BY CreatedAt DESC;
```

---

## 🚀 Next Steps

MVP tamamlandıktan sonra:

1. **Recurring Payments** — İyzico subscription API
2. **Webhook Handler** — Otomatik yenileme
3. **Payment Retry** — Failed payment recovery
4. **Cancellation Flow** — /settings/billing sayfası
5. **Premium Features** — Product boosting, badge, etc.

---

**Estimated Time:** 2-3 gün (MVP), 10 gün (Full)

**Ready to code!** 🎯
