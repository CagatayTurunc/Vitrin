using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vitrin.Auth.Application.Interfaces;
using Vitrin.Auth.Domain.Entities;
using Vitrin.Auth.Infrastructure.Data;
using Vitrin.Shared.Contracts.Events;
using Vitrin.Shared.Contracts.Payment;
using Vitrin.Shared.Infrastructure.Kafka;
using DomainSubscriptionTier = Vitrin.Auth.Domain.Entities.SubscriptionTier;
using DomainPaymentStatus = Vitrin.Auth.Domain.Entities.PaymentStatus;
using ContractSubscriptionTier = Vitrin.Shared.Contracts.Payment.SubscriptionTier;

namespace Vitrin.Auth.Infrastructure.Services;

/// <summary>
/// Her gün UTC 02:00'da çalışır.
/// Dönem biten aktif abonelikleri bulur, İyzico'ya yenileme ödemesi gönderir.
///
/// Başarılı ödeme:
///   → RenewBillingCycle() → PaymentHistory oluştur → SubscriptionRenewedEvent yayınla → email gönder
///
/// Başarısız ödeme:
///   → MarkAsPastDue() → PaymentHistory(Failed) → SubscriptionPaymentFailedEvent yayınla
///   → Retry tablosu: 3 gün, 7 gün, 14 gün sonra tekrar dene
///
/// Retry de başarısız olursa ExpirationWorker devreye girer (PastDue + grace period dolunca).
/// </summary>
public sealed class SubscriptionRenewalWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionRenewalWorker> _logger;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

    // Ödeme başarısız olursa kaç gün sonra retry yapılacak
    private static readonly int[] RetryDays = [3, 7, 14];

    public SubscriptionRenewalWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<SubscriptionRenewalWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SubscriptionRenewalWorker başlatıldı.");

        // İlk çalışmayı bir sonraki UTC 02:00'a ertele
        await WaitUntilNextRunAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunRenewalAsync(stoppingToken);
            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task RunRenewalAsync(CancellationToken ct)
    {
        _logger.LogInformation("Abonelik yenileme döngüsü başlıyor: {Time}", DateTime.UtcNow);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
            var emailService = scope.ServiceProvider.GetRequiredService<IAccountEmailService>();
            var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            var now = DateTime.UtcNow;
            var todayEnd = now.Date.AddDays(1); // bugün gece yarısına kadar

            // ──────────────────────────────────────────────────────
            // 1. Dönem biten aktif abonelikleri bul (bugün veya daha önce)
            //    CancelAtPeriodEnd = true olanlar → expire edilecek (renewal yok)
            // ──────────────────────────────────────────────────────
            var dueForRenewal = await db.Subscriptions
                .Where(s =>
                    s.Status == SubscriptionStatus.Active &&
                    s.Tier != DomainSubscriptionTier.Free &&
                    !s.CancelAtPeriodEnd &&
                    s.CurrentPeriodEnd <= todayEnd &&
                    s.IyzicoCustomerId != null &&
                    s.IyzicoSubscriptionId != null)
                .ToListAsync(ct);

            _logger.LogInformation("Yenilenecek abonelik sayısı: {Count}", dueForRenewal.Count);

            foreach (var subscription in dueForRenewal)
            {
                await ProcessRenewalAsync(subscription, db, paymentService, emailService, eventPublisher, ct);
            }

            // ──────────────────────────────────────────────────────
            // 2. CancelAtPeriodEnd = true, süresi dolmuş → expire et
            // ──────────────────────────────────────────────────────
            var dueForExpiry = await db.Subscriptions
                .Where(s =>
                    s.Status == SubscriptionStatus.Active &&
                    s.Tier != DomainSubscriptionTier.Free &&
                    s.CancelAtPeriodEnd &&
                    s.CurrentPeriodEnd <= now)
                .ToListAsync(ct);

            _logger.LogInformation("Süresi dolan iptal abonelik sayısı: {Count}", dueForExpiry.Count);

            foreach (var subscription in dueForExpiry)
            {
                await ExpireSubscriptionAsync(subscription, db, emailService, eventPublisher, ct);
            }

            // ──────────────────────────────────────────────────────
            // 3. PastDue aboneliklerde retry zamanı gelenleri yeniden dene
            // ──────────────────────────────────────────────────────
            var dueForRetry = await db.Subscriptions
                .Where(s =>
                    s.Status == SubscriptionStatus.PastDue &&
                    s.Tier != DomainSubscriptionTier.Free &&
                    s.IyzicoCustomerId != null &&
                    s.IyzicoSubscriptionId != null)
                .ToListAsync(ct);

            // PaymentHistory'den son başarısız kaydı bul; NextRetryAt geçmişse dene
            foreach (var subscription in dueForRetry)
            {
                var lastFailedPayment = await db.PaymentHistories
                    .Where(p => p.SubscriptionId == subscription.Id && p.Status == DomainPaymentStatus.Failed)
                    .OrderByDescending(p => p.BillingDate)
                    .FirstOrDefaultAsync(ct);

                if (lastFailedPayment?.NextRetryAt is not null && lastFailedPayment.NextRetryAt <= now)
                {
                    await ProcessRenewalAsync(subscription, db, paymentService, emailService, eventPublisher, ct);
                }
            }

            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Abonelik yenileme döngüsü tamamlandı.");
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "SubscriptionRenewalWorker çalışırken hata oluştu.");
        }
    }

    private async Task ProcessRenewalAsync(
        Subscription subscription,
        AuthDbContext db,
        IPaymentService paymentService,
        IAccountEmailService emailService,
        IEventPublisher eventPublisher,
        CancellationToken ct)
    {
        var user = await db.Users.FindAsync([subscription.UserId], ct);
        if (user is null)
        {
            _logger.LogWarning("Kullanıcı bulunamadı: UserId={UserId}", subscription.UserId);
            return;
        }

        // Retry sayısını bul
        var retryCount = await db.PaymentHistories
            .CountAsync(p => p.SubscriptionId == subscription.Id && p.Status == DomainPaymentStatus.Failed, ct);

        var conversationId = Guid.NewGuid().ToString();

        // Ödemeyi dene
        var chargeResult = await paymentService.ChargeStoredCardAsync(new ChargeRequest(
            UserId: subscription.UserId,
            IyzicoCustomerId: subscription.IyzicoCustomerId!,
            IyzicoSubscriptionId: subscription.IyzicoSubscriptionId!,
            Email: user.Email,
            FullName: user.FullName ?? user.Username,
            Tier: (ContractSubscriptionTier)(int)subscription.Tier,
            ConversationId: conversationId), ct);

        if (chargeResult.Success)
        {
            // ── Başarılı ──
            subscription.RenewBillingCycle();

            var paymentHistory = PaymentHistory.Create(
                subscriptionId: subscription.Id,
                userId: subscription.UserId,
                amount: chargeResult.PaidPrice,
                currency: chargeResult.Currency,
                billingDate: DateTime.UtcNow);

            paymentHistory.MarkAsSucceeded(
                iyzicoPaymentId: chargeResult.PaymentId!,
                iyzicoConversationId: chargeResult.ConversationId ?? conversationId);

            db.PaymentHistories.Add(paymentHistory);

            _logger.LogInformation(
                "Abonelik yenilendi: UserId={UserId}, Tier={Tier}, YeniDönemBitiş={End}",
                subscription.UserId, subscription.Tier, subscription.CurrentPeriodEnd);

            // Kafka event
            await eventPublisher.PublishAsync(new SubscriptionRenewedEvent
            {
                UserId = subscription.UserId,
                Tier = subscription.Tier.ToString(),
                NewPeriodEnd = subscription.CurrentPeriodEnd,
                PaidAmount = chargeResult.PaidPrice,
                Currency = chargeResult.Currency
            }, ct);

            // Email — fire and forget
            _ = emailService.SendSubscriptionRenewedAsync(
                user,
                subscription.Tier.ToString(),
                subscription.CurrentPeriodEnd,
                chargeResult.PaidPrice,
                ct).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    _logger.LogError(t.Exception, "Yenileme emaili gönderilemedi: UserId={UserId}", subscription.UserId);
            }, TaskScheduler.Default);
        }
        else
        {
            // ── Başarısız ──
            var paymentHistory = PaymentHistory.Create(
                subscriptionId: subscription.Id,
                userId: subscription.UserId,
                amount: GetTierPrice(subscription.Tier),
                currency: "TRY",
                billingDate: DateTime.UtcNow);

            // Retry programla: 3 → 7 → 14 gün
            DateTime? nextRetryAt = null;
            if (retryCount < RetryDays.Length)
            {
                nextRetryAt = DateTime.UtcNow.AddDays(RetryDays[retryCount]);
                paymentHistory.IncrementRetry(nextRetryAt.Value);
            }

            paymentHistory.MarkAsFailed(
                chargeResult.ErrorMessage ?? "Bilinmeyen hata",
                chargeResult.ErrorCode);

            db.PaymentHistories.Add(paymentHistory);

            // İlk başarısızlıkta PastDue'ya geç
            if (subscription.Status == SubscriptionStatus.Active)
                subscription.MarkAsPastDue();

            _logger.LogWarning(
                "Abonelik ödemesi başarısız: UserId={UserId}, RetryCount={Retry}, NextRetry={Next}, Error={Error}",
                subscription.UserId, retryCount + 1, nextRetryAt, chargeResult.ErrorMessage);

            // Kafka event
            await eventPublisher.PublishAsync(new SubscriptionPaymentFailedEvent
            {
                UserId = subscription.UserId,
                Tier = subscription.Tier.ToString(),
                ErrorCode = chargeResult.ErrorCode,
                ErrorMessage = chargeResult.ErrorMessage,
                RetryCount = retryCount + 1,
                NextRetryAt = nextRetryAt
            }, ct);

            // Email — fire and forget
            _ = emailService.SendPaymentFailedAsync(
                user,
                subscription.Tier.ToString(),
                retryCount + 1,
                nextRetryAt,
                ct).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    _logger.LogError(t.Exception, "Ödeme başarısız emaili gönderilemedi: UserId={UserId}", subscription.UserId);
            }, TaskScheduler.Default);
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task ExpireSubscriptionAsync(
        Subscription subscription,
        AuthDbContext db,
        IAccountEmailService emailService,
        IEventPublisher eventPublisher,
        CancellationToken ct)
    {
        var expiredTier = subscription.Tier.ToString();
        subscription.MarkAsExpired();

        _logger.LogInformation(
            "Abonelik expire edildi (iptal planlanmıştı): UserId={UserId}, Tier={Tier}",
            subscription.UserId, expiredTier);

        await eventPublisher.PublishAsync(new SubscriptionExpiredEvent
        {
            UserId = subscription.UserId,
            ExpiredTier = expiredTier,
            ExpiredAt = DateTime.UtcNow
        }, ct);

        var user = await db.Users.FindAsync([subscription.UserId], ct);
        if (user is not null)
        {
            _ = emailService.SendSubscriptionExpiredAsync(user, expiredTier, ct)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        _logger.LogError(t.Exception, "Expire emaili gönderilemedi: UserId={UserId}", subscription.UserId);
                }, TaskScheduler.Default);
        }

        await db.SaveChangesAsync(ct);
    }

    private static decimal GetTierPrice(DomainSubscriptionTier tier) => tier switch
    {
        DomainSubscriptionTier.ProMaker => 299m,
        DomainSubscriptionTier.Enterprise => 999m,
        _ => 0m
    };

    private static async Task WaitUntilNextRunAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var nextRun = now.Date.AddHours(2); // UTC 02:00
        if (nextRun <= now)
            nextRun = nextRun.AddDays(1);

        var delay = nextRun - now;
        await Task.Delay(delay, ct);
    }
}
