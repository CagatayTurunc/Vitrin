using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vitrin.Auth.Application.Interfaces;
using Vitrin.Auth.Infrastructure.Data;

namespace Vitrin.Auth.Infrastructure.Services;

/// <summary>
/// Abonelik dönem sonu yaklaştığında kullanıcıya hatırlatma emaili gönderir.
/// Kural: Dönem bitiminden 3 gün önce, aktif Pro/Enterprise abonelere email gönder.
/// Her gün UTC 09:00'da çalışır.
/// </summary>
public sealed class SubscriptionReminderWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionReminderWorker> _logger;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);
    private const int ReminderDaysBeforeExpiry = 3;

    public SubscriptionReminderWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<SubscriptionReminderWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SubscriptionReminderWorker başlatıldı.");

        // İlk çalışmayı bir sonraki UTC 09:00'a ertele
        await WaitUntilNextRunAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunReminderAsync(stoppingToken);
            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task RunReminderAsync(CancellationToken ct)
    {
        _logger.LogInformation("Abonelik hatırlatma kontrolü başlıyor...");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IAccountEmailService>();

            var reminderDate = DateTime.UtcNow.AddDays(ReminderDaysBeforeExpiry).Date;
            var reminderDateEnd = reminderDate.AddDays(1);

            // Dönem sonu 3 gün içinde olan aktif Pro/Enterprise aboneleri bul
            // CancelAtPeriodEnd = true olanları hariç tut (zaten iptal ettiler, tekrar email gönderme)
            var expiringSubs = await db.Subscriptions
                .Where(s =>
                    s.Status == Vitrin.Auth.Domain.Entities.SubscriptionStatus.Active &&
                    s.Tier != Vitrin.Auth.Domain.Entities.SubscriptionTier.Free &&
                    !s.CancelAtPeriodEnd &&
                    s.CurrentPeriodEnd >= reminderDate &&
                    s.CurrentPeriodEnd < reminderDateEnd)
                .Select(s => new { s.UserId, s.Tier, s.CurrentPeriodEnd })
                .ToListAsync(ct);

            _logger.LogInformation(
                "Hatırlatma emaili gönderilecek abonelik sayısı: {Count}", expiringSubs.Count);

            var successCount = 0;
            var failCount = 0;

            foreach (var sub in expiringSubs)
            {
                try
                {
                    var user = await db.Users.FindAsync([sub.UserId], ct);
                    if (user is null) continue;

                    var sent = await emailService.SendSubscriptionRenewalReminderAsync(
                        user,
                        sub.Tier.ToString(),
                        sub.CurrentPeriodEnd,
                        ct);

                    if (sent) successCount++;
                    else failCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Hatırlatma emaili gönderilemedi. UserId={UserId}", sub.UserId);
                    failCount++;
                }
            }

            _logger.LogInformation(
                "Abonelik hatırlatma tamamlandı. Başarılı={Success}, Başarısız={Fail}",
                successCount, failCount);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "SubscriptionReminderWorker çalışırken hata oluştu.");
        }
    }

    private static async Task WaitUntilNextRunAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var nextRun = now.Date.AddHours(9); // UTC 09:00
        if (nextRun <= now)
            nextRun = nextRun.AddDays(1);

        var delay = nextRun - now;
        await Task.Delay(delay, ct);
    }
}
