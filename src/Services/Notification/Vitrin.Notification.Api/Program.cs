using Microsoft.EntityFrameworkCore;
using Vitrin.Notification.Application.Commands;
using Vitrin.Notification.Infrastructure.Data;
using Vitrin.Notification.Infrastructure.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SendNotificationCommand).Assembly));

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=notification_db.sqlite"));

builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

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

app.MapPost("/api/notifications", async ([FromBody] SendNotificationCommand command, IMediator mediator) =>
{
    var result = await mediator.Send(command);
    if (result.IsSuccess)
    {
        return Results.Ok(new { NotificationId = result.Value, Message = "Notification sent successfully!" });
    }
    return Results.BadRequest(new { Error = result.Error });
})
.WithName("SendNotification")
.WithOpenApi();

app.MapGet("/api/notifications/{userId}", async (Guid userId, NotificationDbContext db) =>
{
    var notifications = await db.Notifications
        .Where(n => n.UserId == userId)
        .OrderByDescending(n => n.CreatedAt)
        .ToListAsync();
    return Results.Ok(notifications);
})
.WithName("GetNotificationsByUser")
.WithOpenApi();

app.MapPost("/api/notifications/{id}/read", async (Guid id, HttpContext context, IMediator mediator) =>
{
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
    if (authHeader == null || !authHeader.StartsWith("Bearer "))
        return Results.Unauthorized();

    var token = authHeader.Substring("Bearer ".Length);
    Guid userId = Guid.Empty;
    try {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(sub, out userId)) return Results.Unauthorized();
    } catch { return Results.Unauthorized(); }

    var result = await mediator.Send(new Vitrin.Notification.Application.Commands.MarkAsReadCommand(id, userId));
    if (result.IsSuccess)
    {
        return Results.Ok(new { Message = "Notification marked as read." });
    }
    return Results.BadRequest(new { Error = result.Error });
})
.WithName("MarkNotificationAsRead")
.WithOpenApi();

app.Run();
