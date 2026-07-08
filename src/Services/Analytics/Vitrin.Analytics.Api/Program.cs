using Microsoft.EntityFrameworkCore;
using Vitrin.Analytics.Application.Commands;
using Vitrin.Analytics.Infrastructure.Data;
using Vitrin.Analytics.Infrastructure.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(TrackEventCommand).Assembly));

builder.Services.AddDbContext<AnalyticsDbContext>(options =>
    options.UseSqlite("Data Source=analytics_db.sqlite"));

builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/api/analytics", async ([FromBody] TrackEventCommand command, IMediator mediator) =>
{
    var result = await mediator.Send(command);
    if (result.IsSuccess)
    {
        return Results.Ok(new { EventId = result.Value, Message = "Event tracked successfully!" });
    }
    return Results.BadRequest(new { Error = result.Error });
})
.WithName("TrackEvent")
.WithOpenApi();

app.MapGet("/api/analytics/product/{productId}", async (Guid productId, AnalyticsDbContext db) =>
{
    var views = await db.AnalyticsEvents
        .CountAsync(a => a.ProductId == productId && a.EventType == "ProductView");
    return Results.Ok(new { ProductId = productId, Views = views });
})
.WithName("GetProductViews")
.WithOpenApi();

app.Run();
