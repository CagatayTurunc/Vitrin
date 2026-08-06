using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vitrin.Auth.Infrastructure.Data;

namespace Vitrin.Auth.Infrastructure.Services;

/// <summary>
/// KVKK Madde 7 — Saklama süresi dolan kullanıcı verilerini anonim hale getirir.
/// Her gün UTC 03:00'da çalışır; DeleteRequestedAtUtc'den 30 gün geçmiş
/// kullanıcıları bulup User.Anonymize() ile kişisel verilerini siler.
/// İstatistiksel veriler (oylar, yorumlar) korunur.
/// </summary>
public sealed class RetentionCleanupWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RetentionCleanupWorker> _logger;

    // Gün içinde bir kez çalışır; üretimde cron-tabanlı bir job'la değiştirilebilir.
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);
    private const int RetentionDays = 30;

    public RetentionCleanupWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<RetentionCleanupWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RetentionCleanupWorker başlatıldı.");

        // Bir sonraki UTC 03:00'a kadar bekle
        await WaitUntilNextRunAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCleanupAsync(stoppingToken);
            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task RunCleanupAsync(CancellationToken ct)
    {
        _logger.LogInformation("KVKK retention temizliği başlıyor...");

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);

            var pendingUsers = await db.Users
                .Where(u => u.DeleteRequestedAtUtc.HasValue
                         && u.DeleteRequestedAtUtc.Value <= cutoff
                         && !u.AnonymizedAtUtc.HasValue)
                .ToListAsync(ct);

            if (pendingUsers.Count == 0)
            {
                _logger.LogInformation("Temizlenecek kullanıcı yok.");
                return;
            }

            var now = DateTime.UtcNow;
            foreach (var user in pendingUsers)
            {
                user.Anonymize(now);
                _logger.LogInformation(
                    "Kullanıcı anonim hale getirildi: {UserId} (talep: {RequestedAt:u})",
                    user.Id, user.DeleteRequestedAtUtc);
            }

            var saved = await db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "KVKK retention temizliği tamamlandı: {Count} kullanıcı anonim hale getirildi.",
                saved);
        }
        catch (OperationCanceledException)
        {
            // Uygulama kapanıyor, normal akış
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KVKK retention temizliği sırasında hata oluştu.");
        }
    }

    /// <summary>Bir sonraki UTC 03:00'a kadar bekler (maksimum 24 saat).</summary>
    private static async Task WaitUntilNextRunAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var next = now.Date.AddHours(3); // bugün 03:00
        if (next <= now) next = next.AddDays(1);   // geçtiyse yarın 03:00

        var delay = next - now;
        await Task.Delay(delay, ct);
    }
}
