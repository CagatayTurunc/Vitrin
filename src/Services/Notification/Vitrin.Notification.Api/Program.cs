using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vitrin.Notification.Application.Commands;
using Vitrin.Notification.Infrastructure;
using Vitrin.Notification.Infrastructure.Data;
using Vitrin.Shared.Infrastructure.Auth;
using Vitrin.Shared.Infrastructure.Api;
using Vitrin.Shared.Infrastructure.Migrations;
using System.Text.Json;
using Vitrin.Notification.Domain.Entities;
using Vitrin.Notification.Infrastructure.Email;
using Vitrin.Notification.Infrastructure.Realtime;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddVitrinSwagger("Vitrin Notification API",
    "Kullanıcı bildirimleri, SSE stream, tercihler ve e-posta digest.");
builder.Services.AddHealthChecks();
builder.Services.AddVitrinJwtAuthentication(builder.Configuration);
builder.Services.AddVitrinApiErrors();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(SendNotificationCommand).Assembly));

// Infrastructure: DbContext + Repository + Kafka Consumer (BackgroundService)
builder.Services.AddNotificationInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseVitrinApiErrors();

if (await app.MigrateDatabaseAndExitAsync<NotificationDbContext>(
    args,
    static (db, cancellationToken) => db.Database.MigrateAsync(cancellationToken))) return;

if (app.Environment.IsDevelopment())
{
    app.UseVitrinSwagger(app.Environment);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

// ─── Endpoints ──────────────────────────────────────────────────────────────

// Manuel bildirim gönder (internal/test kullanım — üretimde Kafka üzerinden gelir)
app.MapPost("/api/notifications", async ([FromBody] SendNotificationRequest request, IMediator mediator) =>
{
    var command = new SendNotificationCommand(request.RecipientUserId, request.Message, request.NotificationType, request.RelatedEntityId);
    var result = await mediator.Send(command);
    return result.IsSuccess
        ? Results.Ok(new { NotificationId = result.Value })
        : ApiProblemResults.BadRequest(result.Error, "notification.send_failed");
})
.WithName("SendNotification")
.WithOpenApi()
.RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

// Kullanıcının bildirimlerini getir
app.MapGet("/api/notifications/me", async (string? type, bool? unread, int? take, HttpContext context, NotificationDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var preference = await db.NotificationPreferences
        .AsNoTracking()
        .FirstOrDefaultAsync(item => item.UserId == userId.Value, context.RequestAborted);
    if (preference?.InAppEnabled == false) return Results.Ok(Array.Empty<object>());

    var query = db.Notifications.AsNoTracking().Where(n => n.UserId == userId.Value);
    if (unread == true) query = query.Where(item => !item.IsRead);
    if (!string.IsNullOrWhiteSpace(type) && !type.Equals("all", StringComparison.OrdinalIgnoreCase))
        query = query.Where(item => item.NotificationType != null && item.NotificationType.StartsWith(type));
    var notifications = await query.OrderByDescending(n => n.CreatedAt)
        .Take(Math.Clamp(take ?? 100, 1, 250)).ToListAsync(context.RequestAborted);
    var visible = (preference is null ? notifications : notifications.Where(item => preference.AllowsType(item.NotificationType)))
        .Select(item => new
        {
            item.Id, item.UserId, item.Message, item.IsRead, item.CreatedAt, item.NotificationType, item.RelatedEntityId,
            ActionUrl = NotificationActionUrl(item.NotificationType, item.RelatedEntityId)
        }).ToList();
    return Results.Ok(visible);
})
.WithName("GetMyNotifications")
.WithOpenApi()
.RequireAuthorization();

// Okunmamış bildirim sayısı
app.MapGet("/api/notifications/me/unread-count", async (HttpContext context, NotificationDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var preference = await db.NotificationPreferences
        .AsNoTracking()
        .FirstOrDefaultAsync(item => item.UserId == userId.Value, context.RequestAborted);
    if (preference?.InAppEnabled == false) return Results.Ok(new { UnreadCount = 0 });

    var unreadTypes = await db.Notifications
        .AsNoTracking()
        .Where(n => n.UserId == userId.Value && !n.IsRead)
        .OrderByDescending(n => n.CreatedAt)
        .Take(500)
        .Select(n => n.NotificationType)
        .ToListAsync(context.RequestAborted);
    var count = preference is null
        ? unreadTypes.Count
        : unreadTypes.Count(preference.AllowsType);
    return Results.Ok(new { UnreadCount = count });
})
.WithName("GetUnreadCount")
.WithOpenApi()
.RequireAuthorization();

// Authorization header destekli Server-Sent Events kanalı.
app.MapGet("/api/notifications/stream", async (
    HttpContext context,
    NotificationDbContext db,
    NotificationStreamBroker broker) =>
{
    var userId = context.User.GetUserId();
    if (userId is null)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    var preference = await db.NotificationPreferences
        .AsNoTracking()
        .FirstOrDefaultAsync(item => item.UserId == userId.Value, context.RequestAborted);
    if (preference?.InAppEnabled == false)
    {
        context.Response.StatusCode = StatusCodes.Status204NoContent;
        return;
    }

    context.Response.ContentType = "text/event-stream";
    context.Response.Headers.CacheControl = "no-cache, no-transform";
    context.Response.Headers.Connection = "keep-alive";
    context.Response.Headers["X-Accel-Buffering"] = "no";
    await context.Response.StartAsync(context.RequestAborted);
    await context.Response.WriteAsync("retry: 3000\n\n", context.RequestAborted);
    await context.Response.Body.FlushAsync(context.RequestAborted);

    await using var subscription = broker.Subscribe(userId.Value);
    var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    while (!context.RequestAborted.IsCancellationRequested)
    {
        var availableTask = subscription.Reader.WaitToReadAsync(context.RequestAborted).AsTask();
        var heartbeatTask = Task.Delay(TimeSpan.FromSeconds(15), context.RequestAborted);
        var completed = await Task.WhenAny(availableTask, heartbeatTask);

        if (completed == heartbeatTask)
        {
            await context.Response.WriteAsync(": heartbeat\n\n", context.RequestAborted);
            await context.Response.Body.FlushAsync(context.RequestAborted);
            continue;
        }

        if (!await availableTask) break;
        while (subscription.Reader.TryRead(out var notification))
        {
            // Kullanıcının bildirim tercihleri bu türe izin vermiyorsa gönderme
            if (preference is not null && !preference.AllowsType(notification.NotificationType))
                continue;

            await context.Response.WriteAsync($"id: {notification.Id}\n", context.RequestAborted);
            await context.Response.WriteAsync("event: notification\n", context.RequestAborted);
            await context.Response.WriteAsync(
                $"data: {JsonSerializer.Serialize(notification, jsonOptions)}\n\n",
                context.RequestAborted);
        }
        await context.Response.Body.FlushAsync(context.RequestAborted);
    }
})
.WithName("StreamMyNotifications")
.RequireAuthorization();

app.MapGet("/api/notifications/preferences", async (HttpContext context, NotificationDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var preference = await db.NotificationPreferences
        .AsNoTracking()
        .FirstOrDefaultAsync(item => item.UserId == userId.Value, context.RequestAborted)
        ?? NotificationPreference.CreateDefault(userId.Value);
    return Results.Ok(ToPreferenceResponse(preference));
})
.WithName("GetNotificationPreferences")
.RequireAuthorization();

app.MapPut("/api/notifications/preferences", async (
    [FromBody] UpdateNotificationPreferencesRequest request,
    HttpContext context,
    NotificationDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();
    if (!Enum.TryParse<EmailDigestFrequency>(request.DigestFrequency, true, out var frequency))
        return ApiProblemResults.BadRequest("Digest frequency is invalid.", "notification.digest_frequency_invalid");
    if (request.EmailEnabled && string.IsNullOrWhiteSpace(request.EmailAddress))
        return ApiProblemResults.BadRequest("Email address is required for email digests.", "notification.email_required");

    var preference = await db.NotificationPreferences
        .FirstOrDefaultAsync(item => item.UserId == userId.Value, context.RequestAborted);
    if (preference is null)
    {
        preference = NotificationPreference.CreateDefault(userId.Value, request.EmailAddress);
        db.NotificationPreferences.Add(preference);
    }

    preference.Update(
        request.EmailAddress,
        request.InAppEnabled,
        request.EmailEnabled,
        frequency,
        request.ProductUpdatesEnabled,
        request.CommentsEnabled,
        request.MentionsEnabled,
        request.ReactionsEnabled,
        request.SocialEnabled,
        request.ModerationEnabled,
        DateTime.UtcNow);
    await db.SaveChangesAsync(context.RequestAborted);
    return Results.Ok(ToPreferenceResponse(preference));
})
.WithName("UpdateNotificationPreferences")
.RequireAuthorization();

app.MapPost("/api/notifications/digest/send-now", async (
    HttpContext context,
    NotificationDigestDispatcher dispatcher) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();
    var count = await dispatcher.SendForUserAsync(userId.Value, true, context.RequestAborted);
    return Results.Ok(new { NotificationCount = count });
})
.WithName("SendNotificationDigestNow")
.RequireAuthorization();

// Bildirimi okundu işaretle
app.MapPost("/api/notifications/{id:guid}/read", async (Guid id, HttpContext context, IMediator mediator) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var result = await mediator.Send(new MarkAsReadCommand(id, userId.Value));
    return result.IsSuccess
        ? Results.Ok(new { Message = "Notification marked as read." })
        : ApiProblemResults.BadRequest(result.Error, "notification.mark_read_failed");
})
.WithName("MarkNotificationAsRead")
.WithOpenApi()
.RequireAuthorization();

// Tüm bildirimleri okundu işaretle
app.MapPost("/api/notifications/read-all", async (HttpContext context, NotificationDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var unread = await db.Notifications
        .Where(n => n.UserId == userId.Value && !n.IsRead)
        .ToListAsync();

    foreach (var n in unread) n.MarkAsRead();
    await db.SaveChangesAsync();

    return Results.Ok(new { MarkedAsRead = unread.Count });
})
.WithName("MarkAllAsRead")
.WithOpenApi()
.RequireAuthorization();

app.MapPost("/api/newsletter/subscribe", async (
    [FromBody] NewsletterSubscriptionRequest request,
    HttpContext context,
    NotificationDbContext db) =>
{
    if (!IsValidEmail(request.EmailAddress))
        return ApiProblemResults.BadRequest("A valid email address is required.", "newsletter.email_invalid");
    var email = request.EmailAddress.Trim().ToLowerInvariant();
    var userId = context.User.GetUserId();
    var subscription = await db.NewsletterSubscriptions.FirstOrDefaultAsync(item => item.EmailAddress == email, context.RequestAborted);
    if (subscription is null)
    {
        subscription = NewsletterSubscription.Create(email, userId);
        db.NewsletterSubscriptions.Add(subscription);
    }
    subscription.Update(userId, request.DailyLaunches, request.WeeklyRoundup, request.ProductUpdates,
        request.UpcomingLaunches, request.AiDigest, request.DeveloperDigest, true);
    await db.SaveChangesAsync(context.RequestAborted);
    return Results.Ok(ToNewsletterResponse(subscription));
})
.WithName("SubscribeNewsletter");

app.MapGet("/api/newsletter/me", async (HttpContext context, NotificationDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();
    var item = await db.NewsletterSubscriptions.AsNoTracking().FirstOrDefaultAsync(subscription => subscription.UserId == userId, context.RequestAborted);
    return item is null ? Results.NoContent() : Results.Ok(ToNewsletterResponse(item));
})
.WithName("GetMyNewsletterSubscription")
.RequireAuthorization();

app.MapDelete("/api/newsletter/me", async (HttpContext context, NotificationDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();
    var item = await db.NewsletterSubscriptions.FirstOrDefaultAsync(subscription => subscription.UserId == userId, context.RequestAborted);
    if (item is null) return Results.NoContent();
    item.Update(userId, item.DailyLaunches, item.WeeklyRoundup, item.ProductUpdates, item.UpcomingLaunches, item.AiDigest, item.DeveloperDigest, false);
    await db.SaveChangesAsync(context.RequestAborted);
    return Results.NoContent();
})
.WithName("UnsubscribeNewsletter")
.RequireAuthorization();

app.Run();

static bool IsValidEmail(string? value)
{
    if (string.IsNullOrWhiteSpace(value) || value.Length > 254) return false;
    try { return new System.Net.Mail.MailAddress(value).Address.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase); }
    catch { return false; }
}

static object ToNewsletterResponse(NewsletterSubscription item) => new
{
    item.EmailAddress,
    item.DailyLaunches,
    item.WeeklyRoundup,
    item.ProductUpdates,
    item.UpcomingLaunches,
    item.AiDigest,
    item.DeveloperDigest,
    item.IsActive,
    item.UpdatedAtUtc
};

static string NotificationActionUrl(string? type, Guid? relatedEntityId)
{
    var normalized = type?.ToLowerInvariant() ?? string.Empty;
    if (normalized.StartsWith("comment")) return relatedEntityId is null ? "/notifications" : $"/product/{relatedEntityId}#comments";
    if (normalized.StartsWith("product")) return relatedEntityId is null ? "/my-products" : $"/my-products?productId={relatedEntityId}";
    if (normalized.StartsWith("saved_search") || normalized.StartsWith("topic_")) return "/discover";
    if (normalized is "follow" or "upvote" || normalized.StartsWith("social")) return "/profile";
    if (normalized.StartsWith("maker")) return "/dashboard";
    return "/notifications";
}

static object ToPreferenceResponse(NotificationPreference preference) => new
{
    preference.EmailAddress,
    preference.InAppEnabled,
    preference.EmailEnabled,
    DigestFrequency = preference.DigestFrequency.ToString().ToLowerInvariant(),
    preference.ProductUpdatesEnabled,
    preference.CommentsEnabled,
    preference.MentionsEnabled,
    preference.ReactionsEnabled,
    preference.SocialEnabled,
    preference.ModerationEnabled,
    preference.LastDigestSentAtUtc,
    preference.UpdatedAtUtc
};

public record SendNotificationRequest(Guid RecipientUserId, string Message, string? NotificationType = null, Guid? RelatedEntityId = null);

public record UpdateNotificationPreferencesRequest(
    string? EmailAddress,
    bool InAppEnabled,
    bool EmailEnabled,
    string DigestFrequency,
    bool ProductUpdatesEnabled,
    bool CommentsEnabled,
    bool MentionsEnabled,
    bool ReactionsEnabled,
    bool SocialEnabled,
    bool ModerationEnabled);

public record NewsletterSubscriptionRequest(
    string EmailAddress,
    bool DailyLaunches,
    bool WeeklyRoundup,
    bool ProductUpdates,
    bool UpcomingLaunches,
    bool AiDigest,
    bool DeveloperDigest);
