using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vitrin.Shared.Infrastructure.Inbox;

namespace Vitrin.Shared.Infrastructure.Outbox;

/// <summary>
/// Haftada bir (Pazar UTC 04:00) işlenmiş Outbox ve Inbox satırlarını temizler.
///
/// Batch Processing görevi:
/// - ProcessedAtUtc dolu ve 7 günden eski Outbox satırları silinir.
/// - DeadLetteredAtUtc dolu ve 30 günden eski Outbox satırları silinir.
/// - ProcessedAtUtc dolu ve 30 günden eski Inbox satırları silinir.
///
/// Neden 7/30 gün?
/// - 7 gün: Kafka retention süresiyle hizalı — replay penceresi geçmiş mesajlar
///   artık yeniden işlenemez.
/// - 30 gün: Dead-letter mesajlar sorun tespiti için daha uzun tutulur.
/// - Inbox 30 gün: Consumer idempotency için; event ID'ler bu süre boyunca
///   tekrar işlenmemesi garantilenir.
///
/// Kullanım: AddVitrinOutbox<TDbContext>() çağrıldığında otomatik kayıt edilir.
/// TDbContext hem OutboxMessage hem InboxMessage DbSet'ini içeriyorsa ikisi de temizlenir.
/// </summary>
public sealed class OutboxCleanupWorker<TDbContext> : BackgroundService
    where TDbContext : DbContext
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxCleanupWorker<TDbContext>> _logger;

    private const int OutboxProcessedRetentionDays   = 7;
    private const int OutboxDeadLetterRetentionDays  = 30;
    private const int InboxProcessedRetentionDays    = 30;

    public OutboxCleanupWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxCleanupWorker<TDbContext>> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "OutboxCleanupWorker<{DbContext}> başlatıldı.",
            typeof(TDbContext).Name);

        // İlk çalışma için bir sonraki Pazar UTC 04:00'a kadar bekle
        await WaitUntilNextRunAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCleanupAsync(stoppingToken);

            // Bir sonraki haftaya kadar bekle (7 gün)
            await Task.Delay(TimeSpan.FromDays(7), stoppingToken);
        }
    }

    private async Task RunCleanupAsync(CancellationToken ct)
    {
        _logger.LogInformation(
            "Outbox/Inbox cleanup başlıyor: {DbContext}", typeof(TDbContext).Name);

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TDbContext>();
            var now = DateTime.UtcNow;

            // ── 1. İşlenmiş Outbox satırlarını temizle ──────────────────────
            var outboxProcessedCutoff = now.AddDays(-OutboxProcessedRetentionDays);
            var outboxProcessedDeleted = await db.Set<OutboxMessage>()
                .Where(m => m.ProcessedAtUtc != null && m.ProcessedAtUtc < outboxProcessedCutoff)
                .ExecuteDeleteAsync(ct);

            // ── 2. Dead-letter Outbox satırlarını temizle ────────────────────
            var outboxDlqCutoff = now.AddDays(-OutboxDeadLetterRetentionDays);
            var outboxDlqDeleted = await db.Set<OutboxMessage>()
                .Where(m => m.DeadLetteredAtUtc != null && m.DeadLetteredAtUtc < outboxDlqCutoff)
                .ExecuteDeleteAsync(ct);

            // ── 3. İşlenmiş Inbox satırlarını temizle ───────────────────────
            // Inbox DbSet sadece consumer servislerde var (Product, Analytics, Notification).
            // Auth gibi producer-only servislerde InboxMessage DbSet yoktur;
            // bu durumda metadataları sorgulayarak güvenle atlarız.
            var inboxDeleted = 0;
            var entityTypes = db.Model.GetEntityTypes()
                .Select(t => t.ClrType)
                .ToHashSet();

            if (entityTypes.Contains(typeof(InboxMessage)))
            {
                var inboxCutoff = now.AddDays(-InboxProcessedRetentionDays);
                inboxDeleted = await db.Set<InboxMessage>()
                    .Where(m => m.ProcessedAtUtc < inboxCutoff)
                    .ExecuteDeleteAsync(ct);
            }

            _logger.LogInformation(
                "Outbox/Inbox cleanup tamamlandı [{DbContext}]: " +
                "Outbox processed={OutboxProcessed}, DLQ={OutboxDlq}, Inbox={Inbox}",
                typeof(TDbContext).Name,
                outboxProcessedDeleted,
                outboxDlqDeleted,
                inboxDeleted);
        }
        catch (OperationCanceledException)
        {
            // Uygulama kapanıyor, normal akış
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Outbox/Inbox cleanup sırasında hata oluştu [{DbContext}].",
                typeof(TDbContext).Name);
        }
    }

    /// <summary>
    /// Bir sonraki Pazar UTC 04:00'a kadar bekler.
    /// Bugün Pazar ama saat geçmişse, bir sonraki Pazar'a bekler.
    /// </summary>
    private static async Task WaitUntilNextRunAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // Bugünden itibaren bir sonraki Pazar'ı bul
        var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)now.DayOfWeek + 7) % 7;
        if (daysUntilSunday == 0) daysUntilSunday = 7; // Bugün Pazar → gelecek hafta

        var nextSunday = now.Date.AddDays(daysUntilSunday).AddHours(4); // 04:00 UTC

        // Eğer bugün Pazar ve henüz 04:00 geçmediyse bugün çalış
        if (now.DayOfWeek == DayOfWeek.Sunday)
        {
            var todayRun = now.Date.AddHours(4);
            if (todayRun > now)
                nextSunday = todayRun;
        }

        var delay = nextSunday - now;
        if (delay > TimeSpan.Zero)
            await Task.Delay(delay, ct);
    }
}
