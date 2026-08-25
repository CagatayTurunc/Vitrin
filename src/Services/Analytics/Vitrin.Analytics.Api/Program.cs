using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vitrin.Analytics.Application.Commands;
using Vitrin.Analytics.Application.Queries;
using Vitrin.Analytics.Domain.Repositories;
using Vitrin.Analytics.Infrastructure;
using Vitrin.Analytics.Infrastructure.Data;
using Vitrin.Shared.Infrastructure.Auth;
using Vitrin.Shared.Infrastructure.Api;
using Vitrin.Shared.Infrastructure.Migrations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddVitrinSwagger("Vitrin Analytics API",
    "Event ingestion, ürün ve platform analitik sorguları, maker dashboard istatistikleri.");
builder.Services.AddHealthChecks();
builder.Services.AddVitrinJwtAuthentication(builder.Configuration);
builder.Services.AddVitrinApiErrors();

// MediatR — Application assembly (Commands + Queries)
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(TrackEventCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(GetProductSummaryQuery).Assembly);
});

// Infrastructure: DbContext + Repository + Kafka Consumer (BackgroundService)
builder.Services.AddAnalyticsInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseVitrinApiErrors();

if (await app.MigrateDatabaseAndExitAsync<AnalyticsDbContext>(
    args,
    static (db, cancellationToken) => db.Database.MigrateAsync(cancellationToken))) return;

if (app.Environment.IsDevelopment())
{
    app.UseVitrinSwagger(app.Environment);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

// ─── Commands ──────────────────────────────────────────────────────────────

// Manuel event kayıt (test / internal kullanım)
app.MapPost("/api/analytics/events", async (HttpContext context, [FromBody] TrackEventRequest request, IMediator mediator) =>
{
    var command = new TrackEventCommand(
        request.EventType,
        request.EventData,
        request.ProductId,
        context.User.GetUserId());
    var result = await mediator.Send(command);
    return result.IsSuccess
        ? Results.Ok(new { EventId = result.Value })
        : ApiProblemResults.BadRequest(result.Error, "analytics.event_rejected");
})
.WithName("TrackEvent")
.WithOpenApi()
.RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

// ─── Product Queries ────────────────────────────────────────────────────────

// Ürün analytics özeti (views + upvotes + comments)
app.MapGet("/api/analytics/product/{productId:guid}/summary", async (Guid productId, IMediator mediator) =>
{
    var result = await mediator.Send(new GetProductSummaryQuery(productId));
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : ApiProblemResults.BadRequest(result.Error, "analytics.query_failed");
})
.WithName("GetProductSummary")
.WithOpenApi();

// Ürün görüntülenme sayısı
app.MapGet("/api/analytics/product/{productId:guid}/views", async (Guid productId, IMediator mediator) =>
{
    var result = await mediator.Send(new GetProductSummaryQuery(productId));
    return result.IsSuccess
        ? Results.Ok(new { ProductId = productId, Views = result.Value.Views })
        : ApiProblemResults.BadRequest(result.Error, "analytics.query_failed");
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
        : ApiProblemResults.BadRequest(result.Error, "analytics.query_failed");
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
        : ApiProblemResults.BadRequest(result.Error, "analytics.query_failed");
})
.WithName("GetTopSearches")
.WithOpenApi()
.RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

// ─── Platform Queries ───────────────────────────────────────────────────────

// Platform geneli özet istatistikler (admin paneli için)
app.MapGet("/api/analytics/platform/summary", async (IMediator mediator) =>
{
    var result = await mediator.Send(new GetPlatformSummaryQuery());
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : ApiProblemResults.BadRequest(result.Error, "analytics.query_failed");
})
.WithName("GetPlatformSummary")
.WithOpenApi()
.RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

// ─── Maker Dashboard Queries ────────────────────────────────────────────────

// Bir maker'ın tüm ürünleri için özet (productId listesi alır, gateway üzerinden çağrılır)
app.MapPost("/api/analytics/maker/products", async (
    [FromBody] MakerProductsRequest request,
    IAnalyticsRepository repository,
    HttpContext context) =>
{
    if (request.ProductIds is null || request.ProductIds.Count == 0)
        return Results.Ok(Array.Empty<object>());
    if (request.ProductIds.Count > 50)
        return ApiProblemResults.BadRequest("En fazla 50 ürün sorgulanabilir.", "analytics.too_many_products");

    var stats = await repository.GetMakerProductStatsAsync(request.ProductIds, context.RequestAborted);
    return Results.Ok(stats);
})
.WithName("GetMakerProductStats")
.RequireAuthorization();

// Günlük time-series: views, upvotes, comments
app.MapGet("/api/analytics/product/{productId:guid}/timeseries", async (
    Guid productId,
    [FromQuery] string? metric,
    [FromQuery] int? days,
    IAnalyticsRepository repository,
    HttpContext context) =>
{
    var d = Math.Clamp(days ?? 30, 1, 90);
    var from = DateTime.UtcNow.AddDays(-d);
    var to = DateTime.UtcNow;

    var eventType = (metric?.ToLowerInvariant() ?? "views") switch
    {
        "upvotes" => "ProductUpvote",
        "comments" => "Comment",
        _ => "ProductView"
    };

    var series = await repository.GetDailyTimeSeriesAsync(productId, eventType, from, to, context.RequestAborted);
    return Results.Ok(new { ProductId = productId, Metric = metric ?? "views", Days = d, Series = series });
})
.WithName("GetProductTimeSeries")
.RequireAuthorization();

// Referrer istatistikleri
app.MapGet("/api/analytics/product/{productId:guid}/referrers", async (
    Guid productId,
    [FromQuery] int? days,
    IAnalyticsRepository repository,
    HttpContext context) =>
{
    var d = Math.Clamp(days ?? 30, 1, 90);
    var from = DateTime.UtcNow.AddDays(-d);
    var stats = await repository.GetReferrerStatsAsync(productId, from, context.RequestAborted);
    return Results.Ok(new { ProductId = productId, Days = d, Referrers = stats });
})
.WithName("GetProductReferrers")
.RequireAuthorization();

// Retention istatistikleri
app.MapGet("/api/analytics/product/{productId:guid}/retention", async (
    Guid productId,
    [FromQuery] int? days,
    IAnalyticsRepository repository,
    HttpContext context) =>
{
    var d = Math.Clamp(days ?? 30, 1, 90);
    var from = DateTime.UtcNow.AddDays(-d);
    var stats = await repository.GetRetentionStatsAsync(productId, from, context.RequestAborted);
    return Results.Ok(new { ProductId = productId, Days = d, Retention = stats });
})
.WithName("GetProductRetention")
.RequireAuthorization();

app.Run();

public record TrackEventRequest(string EventType, string EventData, Guid? ProductId = null);
public record MakerProductsRequest(List<Guid> ProductIds);
