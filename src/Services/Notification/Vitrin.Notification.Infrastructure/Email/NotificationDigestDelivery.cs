using System.Net;
using System.Net.Mail;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vitrin.Notification.Domain.Entities;
using Vitrin.Notification.Infrastructure.Data;

namespace Vitrin.Notification.Infrastructure.Email;

public sealed record NotificationDigestEmail(
    string Recipient,
    string Subject,
    string HtmlBody,
    int NotificationCount);

public interface INotificationEmailSender
{
    Task SendAsync(NotificationDigestEmail email, CancellationToken cancellationToken);
}

public sealed class NotificationEmailSender(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<NotificationEmailSender> logger) : INotificationEmailSender
{
    public async Task SendAsync(NotificationDigestEmail email, CancellationToken cancellationToken)
    {
        var resendApiKey = configuration["Email:Resend:ApiKey"];
        if (!string.IsNullOrWhiteSpace(resendApiKey))
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "emails");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", resendApiKey);
            request.Headers.TryAddWithoutValidation("Idempotency-Key", Guid.NewGuid().ToString("N"));
            request.Content = JsonContent.Create(new
            {
                from = configuration["Email:From"] ?? "Vitrin <onboarding@resend.dev>",
                to = new[] { email.Recipient },
                subject = email.Subject,
                html = email.HtmlBody
            });

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode) return;

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Resend request failed with status {(int)response.StatusCode}: {responseBody}");
        }

        var host = configuration["Email:Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            logger.LogInformation(
                "Email SMTP is not configured; digest generated in development. Recipient={Recipient}, Count={Count}",
                email.Recipient,
                email.NotificationCount);
            return;
        }

        var port = configuration.GetValue("Email:Smtp:Port", 587);
        var from = configuration["Email:FromAddress"] ?? "bildirim@vitrin.local";
        var fromName = configuration["Email:FromName"] ?? "Vitrin";
        var username = configuration["Email:Smtp:Username"];
        var password = configuration["Email:Smtp:Password"];

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = configuration.GetValue("Email:Smtp:EnableSsl", true)
        };
        if (!string.IsNullOrWhiteSpace(username))
            client.Credentials = new NetworkCredential(username, password);

        using var message = new MailMessage
        {
            From = new MailAddress(from, fromName),
            Subject = email.Subject,
            Body = email.HtmlBody,
            IsBodyHtml = true
        };
        message.To.Add(email.Recipient);

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
    }
}

public sealed class NotificationDigestDispatcher(
    NotificationDbContext db,
    INotificationEmailSender emailSender,
    TimeProvider timeProvider)
{
    public async Task<int> SendForUserAsync(Guid userId, bool force, CancellationToken cancellationToken)
    {
        var preference = await db.NotificationPreferences
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (preference is null || !preference.EmailEnabled || preference.EmailAddress is null)
            return 0;

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (!force && !preference.IsDigestDue(now)) return 0;

        var windowStart = preference.DigestWindowStart(now);
        var notifications = await db.Notifications
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.CreatedAt > windowStart && item.CreatedAt <= now)
            .OrderByDescending(item => item.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
        notifications = notifications
            .Where(item => preference.AllowsType(item.NotificationType))
            .ToList();

        if (notifications.Count > 0)
        {
            var list = new StringBuilder();
            foreach (var item in notifications)
            {
                list.Append("<li style=\"margin:0 0 12px\"><strong>")
                    .Append(WebUtility.HtmlEncode(item.Message))
                    .Append("</strong><br><small style=\"color:#64748b\">")
                    .Append(item.CreatedAt.ToString("dd.MM.yyyy HH:mm"))
                    .Append(" UTC</small></li>");
            }

            var html = $"""
                <div style="font-family:Arial,sans-serif;max-width:640px;margin:auto;color:#0f172a">
                  <h1 style="font-size:24px">Vitrin bildirim özetin</h1>
                  <p>Kaçırdığın {notifications.Count} gelişme var.</p>
                  <ul style="padding-left:20px">{list}</ul>
                  <p style="color:#64748b;font-size:12px">Bu e-postayı bildirim tercihlerinden kapatabilirsin.</p>
                </div>
                """;
            await emailSender.SendAsync(
                new NotificationDigestEmail(
                    preference.EmailAddress,
                    $"Vitrin: {notifications.Count} yeni bildirim",
                    html,
                    notifications.Count),
                cancellationToken);
        }

        preference.MarkDigestSent(now);
        await db.SaveChangesAsync(cancellationToken);
        return notifications.Count;
    }

    public async Task<int> SendDueBatchAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var candidateIds = await db.NotificationPreferences
            .AsNoTracking()
            .Where(item => item.EmailEnabled && item.DigestFrequency != EmailDigestFrequency.Off)
            .Select(item => item.UserId)
            .Take(250)
            .ToListAsync(cancellationToken);

        var sent = 0;
        foreach (var userId in candidateIds)
        {
            var preference = await db.NotificationPreferences
                .AsNoTracking()
                .FirstAsync(item => item.UserId == userId, cancellationToken);
            if (!preference.IsDigestDue(now)) continue;
            if (await SendForUserAsync(userId, false, cancellationToken) > 0) sent++;
        }
        return sent;
    }
}

public sealed class NotificationDigestWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationDigestWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<NotificationDigestDispatcher>();
                var sent = await dispatcher.SendDueBatchAsync(stoppingToken);
                if (sent > 0) logger.LogInformation("Sent {Count} notification digest emails.", sent);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Notification digest dispatch failed and will be retried.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
        }
    }
}
