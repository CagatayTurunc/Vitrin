using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vitrin.Notification.Application.Commands;
using Vitrin.Notification.Infrastructure;
using Vitrin.Notification.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

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

app.MapHealthChecks("/health");

// ─── Endpoints ──────────────────────────────────────────────────────────────

// Manuel bildirim gönder (internal/test kullanım — üretimde Kafka üzerinden gelir)
app.MapPost("/api/notifications", async ([FromBody] SendNotificationCommand command, IMediator mediator) =>
{
    var result = await mediator.Send(command);
    return result.IsSuccess
        ? Results.Ok(new { NotificationId = result.Value })
        : Results.BadRequest(new { Error = result.Error });
})
.WithName("SendNotification")
.WithOpenApi();

// Kullanıcının bildirimlerini getir
app.MapGet("/api/notifications/{userId:guid}", async (Guid userId, NotificationDbContext db) =>
{
    var notifications = await db.Notifications
        .Where(n => n.UserId == userId)
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
.WithName("GetNotificationsByUser")
.WithOpenApi();

// Okunmamış bildirim sayısı
app.MapGet("/api/notifications/{userId:guid}/unread-count", async (Guid userId, NotificationDbContext db) =>
{
    var count = await db.Notifications
        .CountAsync(n => n.UserId == userId && !n.IsRead);
    return Results.Ok(new { UserId = userId, UnreadCount = count });
})
.WithName("GetUnreadCount")
.WithOpenApi();

// Bildirimi okundu işaretle
app.MapPost("/api/notifications/{id:guid}/read", async (Guid id, HttpContext context, IMediator mediator) =>
{
    var userId = GetUserIdFromToken(context);
    if (userId is null) return Results.Unauthorized();

    var result = await mediator.Send(new MarkAsReadCommand(id, userId.Value));
    return result.IsSuccess
        ? Results.Ok(new { Message = "Notification marked as read." })
        : Results.BadRequest(new { Error = result.Error });
})
.WithName("MarkNotificationAsRead")
.WithOpenApi();

// Tüm bildirimleri okundu işaretle
app.MapPost("/api/notifications/read-all", async (HttpContext context, NotificationDbContext db) =>
{
    var userId = GetUserIdFromToken(context);
    if (userId is null) return Results.Unauthorized();

    var unread = await db.Notifications
        .Where(n => n.UserId == userId.Value && !n.IsRead)
        .ToListAsync();

    foreach (var n in unread) n.MarkAsRead();
    await db.SaveChangesAsync();

    return Results.Ok(new { MarkedAsRead = unread.Count });
})
.WithName("MarkAllAsRead")
.WithOpenApi();

app.Run();

// ─── Yardımcı ───────────────────────────────────────────────────────────────

static Guid? GetUserIdFromToken(HttpContext context)
{
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
    if (authHeader is null || !authHeader.StartsWith("Bearer ")) return null;
    try
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(authHeader["Bearer ".Length..]);
        var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub"
            || c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
    catch { return null; }
}
