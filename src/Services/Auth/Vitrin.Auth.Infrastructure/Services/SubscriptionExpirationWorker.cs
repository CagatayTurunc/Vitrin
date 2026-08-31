using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vitrin.Auth.Application.Interfaces;
using Vitrin.Auth.Domain.Entities;
using Vitrin.Auth.Infrastructure.Data;
using Vitrin.Shared.Contracts.Events;
using Vitrin.Shared.Infrastructure.Kafka;

namespace Vitrin.Auth.Infrastructure.Services;

/// <summary>
/// Her gün UTC 03:00'da çalışır (RetentionCleanupWorker ile çakışmamak için 03:30).
/// PastDue durumundaki aboneliklerde tüm retry'lar başarısız olup
/// grace period (14 gün) da dolduysa aboneliği expire eder.
///
/// Ayrıca herhangi bir nedenle Active kalıp CurrentPeriodEnd'i
/// çok geçmişte olan abonelikleri de temizler (güvenlik ağı).
/// </summary>
public sealed class SubscriptionExpirationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionExpirationWorker> _logger;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

    // Son retry'dan sonra kaç gün daha beklenecek (grace period)
    private const int GracePeriodDays = 14;

    public SubscriptionExpirationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<SubscriptionExpirationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SubscriptionExpirationWorker başlatıldı.");

        await WaitUntilNextRunAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunExpirationAsync(stoppingToken);
            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task RunExpirationAsync(CancellationToken ct)
    {
        _logger.LogInformation("Abonelik expire kontrolü başlıyor: {Time}", DateTime.UtcNow);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IAccountEmailService>();
            var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            var now = DateTime.UtcNow;

            // ──────────────────────────────────────────────────────
            // 1. PastDue aboneliklerde grace period dolan veya
            //    NextRetryAt'i olmayan (retry'ları bitmiş) olanları bul
            // ──────────────────────────────────────────────────────
            var pastDueSubs = await db.Subscriptions
                .Where(s =>
                    s.Status == SubscriptionStatus.PastDue &&
                    s.Tier != SubscriptionTier.Free)
                .ToListAsync(ct);

            foreach (var subscription in pastDueSubs)
            {
                // Son başarısız ödeme kaydına bak
                var lastFailedPayment = await db.PaymentHistories
                    .Where(p => p.SubscriptionId == subscription.Id && p.Status == PaymentStatus.Failed)
                    .OrderByDescending(p => p.BillingDate)
                    .FirstOrDefaultAsync(ct);

                if (lastFailedPayment is null)
                {
                    // Ödeme kaydı yok ama PastDue — güvenlik ağı
                    _logger.LogWarning(
                        "PastDue abonelik için ödeme kaydı bulunamadı: SubId={SubId}, UserId={UserId}",
                        subscription.Id, subscription.UserId);
                    continue;
                }

                var shouldExpire =
                    // Retry'ları bitti (NextRetryAt yok) ve dönem biteli GracePeriodDays gün oldu
                    (lastFailedPayment.NextRetryAt is null &&
                     lastFailedPayment.BillingDate.AddDays(GracePeriodDays) <= now)
                    ||
                    // Son retry zamanı geçmiş ama hâlâ PastDue (RenewalWorker işleyemedi)
                    (lastFailedPayment.NextRetryAt.HasValue &&
                     lastFailedPayment.NextRetryAt.Value.AddDays(GracePeriodDays) <= now);

                if (!shouldExpire) continue;

                await ExpireAsync(subscription, db, emailService, eventPublisher, ct);
            }

            // ──────────────────────────────────────────────────────
            // 2. Güvenlik ağı: Active ama CurrentPeriodEnd 30+ gün geçmiş
            //    (normalde RenewalWorker yakalamalı; bu edge case'leri temizler)
            // ──────────────────────────────────────────────────────
            var staleActiveSubs = await db.Subscriptions
                .Where(s =>
                    s.Status == SubscriptionStatus.Active &&
                    s.Tier != SubscriptionTier.Free &&
                    s.CurrentPeriodEnd <= now.AddDays(-30) &&
                    s.IyzicoCustomerId == null) // Kart bilgisi yok = yenilenemez
                .ToListAsync(ct);

            foreach (var subscription in staleActiveSubs)
            {
                _logger.LogWarning(
                    "Güvenlik ağı: 30+ gün süresi dolmuş Active abonelik expire ediliyor. " +
                    "SubId={SubId}, UserId={UserId}, PeriodEnd={End}",
                    subscription.Id, subscription.UserId, subscription.CurrentPeriodEnd);

                await ExpireAsync(subscription, db, emailService, eventPublisher, ct);
            }

            _logger.LogInformation("Abonelik expire kontrolü tamamlandı.");
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "SubscriptionExpirationWorker çalışırken hata oluştu.");
        }
    }

    private async Task ExpireAsync(
        Subscription subscription,
        AuthDbContext db,
        IAccountEmailService emailService,
        IEventPublisher eventPublisher,
        CancellationToken ct)
    {
        var expiredTier = subscription.Tier.ToString();
        subscription.MarkAsExpired();

        _logger.LogInformation(
            "Abonelik expire edildi: UserId={UserId}, Tier={Tier}",
            subscription.UserId, expiredTier);

        // Kafka event — Product service Free tier'a düşürecek
        await eventPublisher.PublishAsync(new SubscriptionExpiredEvent
        {
            UserId = subscription.UserId,
            ExpiredTier = expiredTier,
            ExpiredAt = DateTime.UtcNow
        }, ct);

        await db.SaveChangesAsync(ct);

        // Email — fire and forget
        var user = await db.Users.FindAsync([subscription.UserId], ct);
        if (user is not null)
        {
            _ = emailService.SendSubscriptionExpiredAsync(user, expiredTier, ct)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        _logger.LogError(t.Exception,
                            "Expire emaili gönderilemedi: UserId={UserId}", subscription.UserId);
                }, TaskScheduler.Default);
        }
    }

    private static async Task WaitUntilNextRunAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var nextRun = now.Date.AddHours(3).AddMinutes(30); // UTC 03:30
        if (nextRun <= now)
            nextRun = nextRun.AddDays(1);

        var delay = nextRun - now;
        await Task.Delay(delay, ct);
    }
}
