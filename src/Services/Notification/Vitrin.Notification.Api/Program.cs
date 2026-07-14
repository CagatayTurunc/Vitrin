using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vitrin.Notification.Application.Commands;
using Vitrin.Notification.Infrastructure;
using Vitrin.Notification.Infrastructure.Data;
using Vitrin.Shared.Infrastructure.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddVitrinJwtAuthentication(builder.Configuration);

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(SendNotificationCommand).Assembly));

// Infrastructure: DbContext + Repository + Kafka Consumer (BackgroundService)
builder.Services.AddNotificationInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

// ─── Endpoints ──────────────────────────────────────────────────────────────

// Manuel bildirim gönder (internal/test kullanım — üretimde Kafka üzerinden gelir)
app.MapPost("/api/notifications", async ([FromBody] SendNotificationRequest request, IMediator mediator) =>
{
    var command = new SendNotificationCommand(request.RecipientUserId, request.Message);
    var result = await mediator.Send(command);
    return result.IsSuccess
        ? Results.Ok(new { NotificationId = result.Value })
        : Results.BadRequest(new { Error = result.Error });
})
.WithName("SendNotification")
.WithOpenApi()
.RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

// Kullanıcının bildirimlerini getir
app.MapGet("/api/notifications/me", async (HttpContext context, NotificationDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var notifications = await db.Notifications
        .Where(n => n.UserId == userId.Value)
        .OrderByDescending(n => n.CreatedAt)
        .Select(n => new
        {
            n.Id,
            n.UserId,
            n.Message,
            n.IsRead,
            n.CreatedAt
        })
        .ToListAsync();

    return Results.Ok(notifications);
})
.WithName("GetMyNotifications")
.WithOpenApi()
.RequireAuthorization();

// Okunmamış bildirim sayısı
app.MapGet("/api/notifications/me/unread-count", async (HttpContext context, NotificationDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var count = await db.Notifications
        .CountAsync(n => n.UserId == userId.Value && !n.IsRead);
    return Results.Ok(new { UnreadCount = count });
})
.WithName("GetUnreadCount")
.WithOpenApi()
.RequireAuthorization();

// Bildirimi okundu işaretle
app.MapPost("/api/notifications/{id:guid}/read", async (Guid id, HttpContext context, IMediator mediator) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var result = await mediator.Send(new MarkAsReadCommand(id, userId.Value));
    return result.IsSuccess
        ? Results.Ok(new { Message = "Notification marked as read." })
        : Results.BadRequest(new { Error = result.Error });
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

app.Run();

public record SendNotificationRequest(Guid RecipientUserId, string Message);
