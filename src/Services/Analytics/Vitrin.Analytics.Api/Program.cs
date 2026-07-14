using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vitrin.Analytics.Application.Commands;
using Vitrin.Analytics.Application.Queries;
using Vitrin.Analytics.Infrastructure;
using Vitrin.Analytics.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// MediatR — Application assembly (Commands + Queries)
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(TrackEventCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(GetProductSummaryQuery).Assembly);
});

// Infrastructure: DbContext + Repository + Kafka Consumer (BackgroundService)
builder.Services.AddAnalyticsInfrastructure(builder.Configuration);

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

app.MapHealthChecks("/health");

// ─── Commands ──────────────────────────────────────────────────────────────

// Manuel event kayıt (test / internal kullanım)
app.MapPost("/api/analytics/events", async ([FromBody] TrackEventCommand command, IMediator mediator) =>
{
    var result = await mediator.Send(command);
    return result.IsSuccess
        ? Results.Ok(new { EventId = result.Value })
        : Results.BadRequest(new { Error = result.Error });
})
.WithName("TrackEvent")
.WithOpenApi();

// ─── Product Queries ────────────────────────────────────────────────────────

// Ürün analytics özeti (views + upvotes + comments)
app.MapGet("/api/analytics/product/{productId:guid}/summary", async (Guid productId, IMediator mediator) =>
{
    var result = await mediator.Send(new GetProductSummaryQuery(productId));
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(new { Error = result.Error });
})
.WithName("GetProductSummary")
.WithOpenApi();

// Ürün görüntülenme sayısı
app.MapGet("/api/analytics/product/{productId:guid}/views", async (Guid productId, IMediator mediator) =>
{
    var result = await mediator.Send(new GetProductSummaryQuery(productId));
    return result.IsSuccess
        ? Results.Ok(new { ProductId = productId, Views = result.Value.Views })
        : Results.BadRequest(new { Error = result.Error });
})
.WithName("GetProductViews")
.WithOpenApi();

// Ürün upvote sayısı
app.MapGet("/api/analytics/product/{productId:guid}/upvotes", async (Guid productId, IMediator mediator) =>
{
    var result = await mediator.Send(new GetProductSummaryQuery(productId));
    return result.IsSuccess
        ? Results.Ok(new
        {
            ProductId  = productId,
            Upvotes    = result.Value.Upvotes,
            Downvotes  = result.Value.Downvotes,
            NetUpvotes = result.Value.NetUpvotes
        })
        : Results.BadRequest(new { Error = result.Error });
})
.WithName("GetProductUpvotes")
.WithOpenApi();

// ─── Search Queries ─────────────────────────────────────────────────────────

// En çok aranan terimler
app.MapGet("/api/analytics/search/top", async (
    IMediator mediator,
    [FromQuery] int limit = 10,
    [FromQuery] DateTime? from = null) =>
{
    var result = await mediator.Send(new GetTopSearchesQuery(limit, from));
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(new { Error = result.Error });
})
.WithName("GetTopSearches")
.WithOpenApi();

// ─── Platform Queries ───────────────────────────────────────────────────────

// Platform geneli özet istatistikler (admin paneli için)
app.MapGet("/api/analytics/platform/summary", async (IMediator mediator) =>
{
    var result = await mediator.Send(new GetPlatformSummaryQuery());
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(new { Error = result.Error });
})
.WithName("GetPlatformSummary")
.WithOpenApi();

app.Run();
