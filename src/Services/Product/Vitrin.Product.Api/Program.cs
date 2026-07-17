using Microsoft.EntityFrameworkCore;
using Vitrin.Product.Application.Commands;
using Vitrin.Product.Infrastructure;
using Vitrin.Product.Infrastructure.Data;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vitrin.Shared.Infrastructure.Auth;
using Vitrin.Shared.Infrastructure.Api;
using Vitrin.Shared.Infrastructure.Audit;
using Vitrin.Shared.Infrastructure.Migrations;
using Vitrin.Shared.Kernel.Pagination;
using Vitrin.Shared.Kernel.Text;
using Vitrin.Product.Api.Products;
using Vitrin.Product.Domain.Entities;
using Vitrin.Product.Infrastructure.Kafka;
using Vitrin.Product.Infrastructure.Repositories;
using NpgsqlTypes;
using Vitrin.Product.Domain.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddVitrinJwtAuthentication(builder.Configuration);
builder.Services.AddVitrinApiErrors();
builder.Services.AddVitrinAuditLogging();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateProductCommand).Assembly));

// Infrastructure: DbContext + Repository + Kafka Producer + Kafka Consumer (VotingEventsConsumer)
builder.Services.AddProductInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseVitrinApiErrors();

if (await app.MigrateDatabaseAndExitAsync<ProductDbContext>(
    args,
    static (db, cancellationToken) => db.Database.MigrateAsync(cancellationToken))) return;

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapPost("/api/products", async (HttpContext context, [FromBody] CreateProductRequest request, IMediator mediator) =>
{
    var makerId = context.User.GetUserId();
    if (makerId is null) return Results.Unauthorized();

    var command = new CreateProductCommand(
        makerId.Value,
        request.Name,
        request.Tagline,
        request.Description,
        request.Slug,
        request.Topics,
        request.ThumbnailUrl,
        request.GalleryUrls,
        request.SaveAsDraft,
        context.User.GetUsername());
    var result = await mediator.Send(command);
    if (result.IsSuccess)
    {
        return Results.Ok(new { ProductId = result.Value, Message = "Product created successfully!" });
    }
    return ApiProblemResults.BadRequest(result.Error, "product.create_failed");
})
.WithName("CreateProduct")
.WithOpenApi()
.RequireAuthorization(VitrinAuthDefaults.MakerOrAdminPolicy);

app.MapGet("/api/products", async (
    string? topicSlug,
    string? topics,
    string? q,
    int? minUpvotes,
    int? minComments,
    int? minViews,
    DateTime? publishedFrom,
    DateTime? publishedTo,
    string? sort,
    string? cursor,
    int? pageSize,
    HttpContext context,
    ProductDbContext db) =>
{
    var requestedPageSize = pageSize ?? 20;
    if (requestedPageSize is < 1 or > 100)
    {
        return ApiProblemResults.BadRequest("Page size must be between 1 and 100.", "pagination.invalid_page_size");
    }

    if (minUpvotes is < 0 || minComments is < 0 || minViews is < 0)
    {
        return ApiProblemResults.BadRequest("Metric filters cannot be negative.", "product.invalid_filter");
    }
    if (publishedFrom is { } filterFrom && publishedTo is { } filterTo && filterFrom.Date > filterTo.Date)
    {
        return ApiProblemResults.BadRequest("Published from cannot be later than published to.", "product.invalid_date_range");
    }

    var normalizedSort = sort?.Trim().ToLowerInvariant() ?? "newest";
    if (normalizedSort is not ("newest" or "trending" or "most_voted" or "most_commented" or "most_viewed"))
    {
        return ApiProblemResults.BadRequest("The requested product sort is invalid.", "product.invalid_sort");
    }

    SortedKeysetCursor? decodedCursor = null;
    if (cursor is not null)
    {
        if (!SortedKeysetCursorCodec.TryDecode(cursor, out var parsedCursor) || parsedCursor.Sort != normalizedSort)
        {
            return ApiProblemResults.BadRequest("The pagination cursor is invalid.", "pagination.invalid_cursor");
        }

        decodedCursor = parsedCursor;
    }

    var query = db.Products
        .AsNoTracking()
        .Where(product => product.Status == ProductStatus.Published);

    var selectedTopicSlugs = (topics ?? topicSlug ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(value => value.ToLowerInvariant())
        .Distinct(StringComparer.Ordinal)
        .Take(20)
        .ToList();
    if (selectedTopicSlugs.Count > 0)
    {
        query = query.Where(product => product.Topics.Any(topic => selectedTopicSlugs.Contains(topic.Slug)));
    }

    var searchTerm = q?.Trim();
    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
        if (searchTerm.Length > 100)
        {
            return ApiProblemResults.BadRequest("Search query cannot exceed 100 characters.", "search.query_too_long");
        }

        query = query.Where(product =>
            EF.Property<NpgsqlTsVector>(product, "SearchVector")
                .Matches(EF.Functions.WebSearchToTsQuery("simple", searchTerm)) ||
            EF.Functions.TrigramsSimilarity(product.Name, searchTerm) >= 0.18 ||
            EF.Functions.TrigramsSimilarity(product.Tagline, searchTerm) >= 0.16 ||
            product.Topics.Any(topic => EF.Functions.TrigramsSimilarity(topic.Name, searchTerm) >= 0.2));
    }

    var filterScope = CreateProductFilterScope(
        searchTerm,
        selectedTopicSlugs,
        minUpvotes,
        minComments,
        minViews,
        publishedFrom,
        publishedTo);
    if (decodedCursor is { } scopedCursor && scopedCursor.Scope != filterScope)
    {
        return ApiProblemResults.BadRequest(
            "The pagination cursor does not belong to these filters.",
            "pagination.cursor_filter_mismatch");
    }

    if (minUpvotes is { } votes) query = query.Where(product => product.Upvotes.Count >= votes);
    if (minComments is { } comments) query = query.Where(product => product.CommentCount >= comments);
    if (minViews is { } views) query = query.Where(product => product.ViewCount >= views);
    if (publishedFrom is { } from)
    {
        var utcFrom = DateTime.SpecifyKind(from, DateTimeKind.Utc);
        query = query.Where(product => product.PublishedAt >= utcFrom);
    }
    if (publishedTo is { } to)
    {
        var utcTo = DateTime.SpecifyKind(to, DateTimeKind.Utc);
        query = to.TimeOfDay == TimeSpan.Zero
            ? query.Where(product => product.PublishedAt < utcTo.AddDays(1))
            : query.Where(product => product.PublishedAt <= utcTo);
    }

    var anchorUtc = decodedCursor?.AnchorUtc ?? DateTime.UtcNow;
    List<ProductResponse> rows;
    if (normalizedSort == "trending")
    {
        var candidates = await query
            .OrderByDescending(product => product.PublishedAt)
            .ThenByDescending(product => product.Id)
            .Take(2_000)
            .ProjectToResponse()
            .ToListAsync(context.RequestAborted);

        var ordered = candidates
            .Select(product => product with { TrendScore = CalculateTrendScore(product, anchorUtc) })
            .OrderByDescending(product => product.TrendScore)
            .ThenByDescending(product => product.PublishedAt)
            .ThenByDescending(product => product.Id);

        rows = (decodedCursor is { } trendCursor
                ? ordered.Where(product => IsAfterCursor(product, product.TrendScore, trendCursor))
                : ordered)
            .Take(requestedPageSize + 1)
            .ToList();
    }
    else
    {
        if (decodedCursor is { } keyset)
        {
            if (normalizedSort == "newest")
            {
                query = query.Where(product =>
                    product.PublishedAt < keyset.TimestampUtc ||
                    (product.PublishedAt == keyset.TimestampUtc && product.Id.CompareTo(keyset.Id) < 0));
            }
            else
            {
                if (keyset.Value < 0 || keyset.Value > int.MaxValue || keyset.Value != Math.Truncate(keyset.Value))
                {
                    return ApiProblemResults.BadRequest("The pagination cursor is invalid.", "pagination.invalid_cursor");
                }

                var metric = (int)keyset.Value;
                query = normalizedSort switch
                {
                    "most_voted" => query.Where(product =>
                        product.Upvotes.Count < metric ||
                        (product.Upvotes.Count == metric && (product.PublishedAt < keyset.TimestampUtc ||
                         (product.PublishedAt == keyset.TimestampUtc && product.Id.CompareTo(keyset.Id) < 0)))),
                    "most_commented" => query.Where(product =>
                        product.CommentCount < metric ||
                        (product.CommentCount == metric && (product.PublishedAt < keyset.TimestampUtc ||
                         (product.PublishedAt == keyset.TimestampUtc && product.Id.CompareTo(keyset.Id) < 0)))),
                    _ => query.Where(product =>
                        product.ViewCount < metric ||
                        (product.ViewCount == metric && (product.PublishedAt < keyset.TimestampUtc ||
                         (product.PublishedAt == keyset.TimestampUtc && product.Id.CompareTo(keyset.Id) < 0))))
                };
            }
        }

        var orderedQuery = normalizedSort switch
        {
            "most_voted" => query.OrderByDescending(product => product.Upvotes.Count)
                .ThenByDescending(product => product.PublishedAt).ThenByDescending(product => product.Id),
            "most_commented" => query.OrderByDescending(product => product.CommentCount)
                .ThenByDescending(product => product.PublishedAt).ThenByDescending(product => product.Id),
            "most_viewed" => query.OrderByDescending(product => product.ViewCount)
                .ThenByDescending(product => product.PublishedAt).ThenByDescending(product => product.Id),
            _ => query.OrderByDescending(product => product.PublishedAt).ThenByDescending(product => product.Id)
        };

        rows = await orderedQuery
            .Take(requestedPageSize + 1)
            .ProjectToResponse()
            .ToListAsync(context.RequestAborted);
        rows = rows.Select(product => product with { TrendScore = CalculateTrendScore(product, anchorUtc) }).ToList();
    }

    var hasMore = rows.Count > requestedPageSize;
    var items = rows.Take(requestedPageSize).ToList();
    var lastItem = items.LastOrDefault();
    var nextCursor = hasMore && lastItem?.PublishedAt is { } publishedAt
        ? SortedKeysetCursorCodec.Encode(
            normalizedSort,
            GetSortValue(lastItem, normalizedSort),
            publishedAt,
            lastItem.Id,
            anchorUtc,
            filterScope)
        : null;

    return Results.Ok(new CursorPage<ProductResponse>(items, nextCursor, hasMore));
})
.WithName("GetProducts")
.WithOpenApi();

app.MapGet("/api/topics", async (ProductDbContext db) =>
{
    var topics = await db.Topics
        .AsNoTracking()
        .OrderBy(topic => topic.Name)
        .Select(topic => new TopicResponse(topic.Id, topic.Name, topic.Slug))
        .ToListAsync();
    return Results.Ok(topics);
})
.WithName("GetTopics")
.WithOpenApi();

app.MapGet("/api/products/trending", async (
    string? period,
    int? limit,
    HttpContext context,
    ProductDbContext db) =>
{
    var requestedLimit = Math.Clamp(limit ?? 12, 1, 50);
    var now = DateTime.UtcNow;
    var normalizedPeriod = period?.Trim().ToLowerInvariant() ?? "7d";
    var from = normalizedPeriod switch
    {
        "24h" => now.AddHours(-24),
        "7d" => now.AddDays(-7),
        "30d" => now.AddDays(-30),
        "all" => DateTime.MinValue,
        _ => now.AddDays(-7)
    };

    var candidates = await db.Products
        .AsNoTracking()
        .Where(product => product.Status == ProductStatus.Published && product.PublishedAt >= from)
        .OrderByDescending(product => product.PublishedAt)
        .Take(300)
        .ProjectToResponse()
        .ToListAsync(context.RequestAborted);

    var trending = candidates
        .Select(product => product with { TrendScore = CalculateTrendScore(product, now) })
        .OrderByDescending(product => product.TrendScore)
        .ThenByDescending(product => product.Upvotes)
        .ThenByDescending(product => product.PublishedAt)
        .Take(requestedLimit)
        .ToList();

    return Results.Ok(new
    {
        Period = normalizedPeriod is "24h" or "7d" or "30d" or "all" ? normalizedPeriod : "7d",
        Formula = ProductTrendScore.Formula,
        ComputedAt = now,
        Items = trending
    });
})
.WithName("GetTrendingProducts")
.WithOpenApi();

app.MapGet("/api/products/{slug}", async (
    string slug,
    HttpContext context,
    ProductDbContext db,
    ProductEventPublisher eventPublisher) =>
{
    var product = await db.Products
        .AsNoTracking()
        .Where(p => p.Status == Vitrin.Product.Domain.Entities.ProductStatus.Published && p.Slug == slug)
        .ProjectToResponse()
        .FirstOrDefaultAsync();

    if (product == null) return Results.NotFound();

    eventPublisher.EnqueueProductViewed(
        product.Id,
        product.Slug,
        context.User.GetUserId(),
        context.Connection.RemoteIpAddress?.ToString(),
        context.Request.Headers.UserAgent.ToString(),
        context.Request.Headers.Referer.ToString());
    await db.SaveChangesAsync(context.RequestAborted);

    return Results.Ok(product with { TrendScore = CalculateTrendScore(product, DateTime.UtcNow) });
})
.WithName("GetProductBySlug")
.WithOpenApi();

app.MapGet("/api/products/batch", async (string ids, ProductDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(ids)) return Results.Ok(new List<object>());
    if (ids.Length > 4_000)
    {
        return ApiProblemResults.BadRequest("The batch query is too large.", "product.batch_limit_exceeded");
    }
    
    var idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(idStr => Guid.TryParse(idStr, out var g) ? g : Guid.Empty)
                    .Where(g => g != Guid.Empty)
                    .ToList();

    if (idList.Count > 100)
    {
        return ApiProblemResults.BadRequest("A batch can contain at most 100 product ids.", "product.batch_limit_exceeded");
    }
                    
    var products = await db.Products
        .AsNoTracking()
        .Where(p => idList.Contains(p.Id))
        .Select(p => new {
            p.Id,
            p.Name,
            p.Slug,
            p.Tagline,
            p.ThumbnailUrl,
            Upvotes = p.Upvotes.Count
        })
        .ToListAsync();
        
    return Results.Ok(products);
})
.WithName("GetProductsBatch")
.WithOpenApi();

app.MapGet("/api/products/compare", async (string ids, HttpContext context, ProductDbContext db) =>
{
    var requestedIds = ids
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(value => Guid.TryParse(value, out var id) ? id : Guid.Empty)
        .Where(id => id != Guid.Empty)
        .Distinct()
        .ToList();

    if (requestedIds.Count is < 1 or > 4)
    {
        return ApiProblemResults.BadRequest(
            "Choose between one and four products to compare.",
            "product.invalid_comparison_size");
    }

    var now = DateTime.UtcNow;
    var products = await db.Products
        .AsNoTracking()
        .Where(product => requestedIds.Contains(product.Id) && product.Status == ProductStatus.Published)
        .ProjectToResponse()
        .ToListAsync(context.RequestAborted);
    var byId = products.ToDictionary(product => product.Id);
    var ordered = requestedIds
        .Where(byId.ContainsKey)
        .Select(id => byId[id] with { TrendScore = CalculateTrendScore(byId[id], now) })
        .ToList();

    return Results.Ok(new
    {
        Items = ordered,
        MaxProducts = 4,
        ComputedAt = now
    });
})
.WithName("CompareProducts")
.WithOpenApi();

app.MapGet("/api/products/my-votes", async (HttpContext context, ProductDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId == null) return Results.Unauthorized();

    var votedProductIds = await db.ProductUpvotes
        .AsNoTracking()
        .Where(u => u.UserId == userId)
        .Select(u => u.ProductItemId)
        .ToListAsync();

    return Results.Ok(votedProductIds);
})
.WithName("GetMyVotes")
.WithOpenApi()
.RequireAuthorization();

app.MapGet("/api/products/upvoted", async (HttpContext context, ProductDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId == null) return Results.Unauthorized();

    var votedProductIds = await db.ProductUpvotes
        .AsNoTracking()
        .Where(u => u.UserId == userId)
        .Select(u => u.ProductItemId)
        .ToListAsync();

    var products = await db.Products
        .AsNoTracking()
        .Where(p => votedProductIds.Contains(p.Id) && p.Status == Vitrin.Product.Domain.Entities.ProductStatus.Published)
        .OrderByDescending(product => product.PublishedAt)
        .Take(100)
        .ProjectToResponse()
        .ToListAsync();
        
    return Results.Ok(products);
})
.WithName("GetUpvotedProducts")
.WithOpenApi()
.RequireAuthorization();

app.MapGet("/api/products/maker/{makerId}", async (Guid makerId, ProductDbContext db) =>
{
    var products = await db.Products
        .AsNoTracking()
        .Where(p => p.MakerId == makerId && p.Status == Vitrin.Product.Domain.Entities.ProductStatus.Published)
        .OrderByDescending(product => product.PublishedAt)
        .Take(100)
        .ProjectToResponse()
        .ToListAsync();
        
    return Results.Ok(products);
})
.WithName("GetMakerProducts")
.WithOpenApi();

app.MapGet("/api/products/admin/pending", async (ProductDbContext db) =>
{
    // ProductStatus.UnderReview is 1
    var products = await db.Products
        .AsNoTracking()
        .Where(p => p.Status == Vitrin.Product.Domain.Entities.ProductStatus.UnderReview)
        .OrderBy(product => product.CreatedAt)
        .Take(100)
        .Select(p => new {
            p.Id, p.Name, p.Slug, p.Tagline, p.Description, p.ThumbnailUrl, p.GalleryUrls, p.MakerId, p.Status, p.CreatedAt, p.PublishedAt, p.ScheduledLaunchAt
        })
        .ToListAsync();
    return Results.Ok(products);
})
.WithName("GetPendingProducts")
.WithOpenApi()
.RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

app.MapPost("/api/products/admin/{id}/approve", async (Guid id, HttpContext context, ProductDbContext db, ProductEventPublisher eventPublisher, IAuditLogger auditLogger) =>
{
    var product = await db.Products.FindAsync(id);
    if (product == null) return Results.NotFound();

    var now = DateTime.UtcNow;
    var result = product.Approve(now);
    if (result.IsFailure) return ApiProblemResults.BadRequest(result.Error, "product.approve_failed");

    if (product.Status == ProductStatus.Published)
    {
        eventPublisher.EnqueueProductPublished(new Vitrin.Shared.Contracts.Events.ProductPublishedEvent
        {
            ProductId = product.Id,
            MakerId = product.MakerId,
            ProductName = product.Name,
            ProductSlug = product.Slug
        });
    }

    eventPublisher.EnqueueProductApprovedNotification(
        product.MakerId,
        product.Name,
        product.Id,
        product.Status == ProductStatus.Scheduled ? product.ScheduledLaunchAt : null);

    await AddRevisionAsync(
        db,
        product,
        context.User.GetUserId()!.Value,
        context.User.GetUsername(),
        product.Status == ProductStatus.Scheduled ? "scheduled" : "approved",
        product.Status == ProductStatus.Scheduled
            ? $"Launch scheduled for {product.ScheduledLaunchAt:O}."
            : "Product approved and published.",
        context.RequestAborted);

    await db.SaveChangesAsync(context.RequestAborted);

    await auditLogger.WriteAsync(
        new AuditEvent("admin.product_approved", context.User.GetUserId(), "Product", id.ToString(), "Succeeded", context.TraceIdentifier),
        context.RequestAborted);

    return Results.Ok(new { Message = "Product approved successfully!" });
})
.WithName("ApproveProduct")
.WithOpenApi()
.RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

app.MapPost("/api/products/admin/{id}/reject", async (Guid id, [FromBody] RejectProductRequest request, HttpContext context, ProductDbContext db, ProductEventPublisher eventPublisher, IAuditLogger auditLogger) =>
{
    var product = await db.Products.FindAsync(id);
    if (product == null) return Results.NotFound();
    
    var result = product.Reject(request.Reason);
    if (result.IsFailure) return ApiProblemResults.BadRequest(result.Error, "product.reject_failed");

    // Product state + SendNotificationEvent atomik commit
    eventPublisher.EnqueueProductRejectedNotification(
        product.MakerId,
        product.Name,
        product.Id,
        product.RejectionReason!);

    await AddRevisionAsync(
        db,
        product,
        context.User.GetUserId()!.Value,
        context.User.GetUsername(),
        "rejected",
        product.RejectionReason,
        context.RequestAborted);

    await db.SaveChangesAsync(context.RequestAborted);

    await auditLogger.WriteAsync(
        new AuditEvent("admin.product_rejected", context.User.GetUserId(), "Product", id.ToString(), "Succeeded", context.TraceIdentifier),
        context.RequestAborted);
    return Results.Ok(new { Message = "Product rejected successfully!" });
})
.WithName("RejectProduct")
.WithOpenApi()
.RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

app.MapGet("/api/products/my-products", async (HttpContext context, ProductDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var products = await db.Products
        .AsNoTracking()
        .Where(p => p.MakerId == userId.Value || p.TeamMembers.Any(member => member.UserId == userId.Value))
        .OrderByDescending(p => p.CreatedAt)
        .Take(200)
        .Select(p => new MyProductResponse(
            p.Id,
            p.Name,
            p.Slug,
            p.Tagline,
            p.ThumbnailUrl,
            p.Status,
            p.RejectionReason,
            p.CreatedAt,
            p.PublishedAt,
            p.ArchivedAt,
            p.ScheduledLaunchAt,
            p.Upvotes.Count,
            p.MakerId == userId.Value,
            p.MakerId == userId.Value
                ? null
                : p.TeamMembers
                    .Where(member => member.UserId == userId.Value)
                    .Select(member => (ProductTeamRole?)member.Role)
                    .FirstOrDefault()))
        .ToListAsync(context.RequestAborted);

    return Results.Ok(products);
})
.WithName("GetMyProducts")
.WithOpenApi()
.RequireAuthorization(VitrinAuthDefaults.MakerOrAdminPolicy);

app.MapPost("/api/products/{id}/submit", async (Guid id, HttpContext context, IMediator mediator) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var result = await mediator.Send(new Vitrin.Product.Application.Commands.SubmitForReviewCommand(id, userId.Value, context.User.GetUsername()), context.RequestAborted);
    if (result.IsFailure) return ApiProblemResults.BadRequest(result.Error, "product.submit_failed");

    return Results.Ok(new { Message = "Product submitted for review." });
})
.WithName("SubmitProductForReview")
.WithOpenApi()
.RequireAuthorization(VitrinAuthDefaults.MakerOrAdminPolicy);

app.MapPost("/api/products/{id}/retract", async (Guid id, HttpContext context, IMediator mediator) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var result = await mediator.Send(new Vitrin.Product.Application.Commands.RetractToDraftCommand(id, userId.Value, context.User.GetUsername()), context.RequestAborted);
    if (result.IsFailure) return ApiProblemResults.BadRequest(result.Error, "product.retract_failed");

    return Results.Ok(new { Message = "Product retracted to draft." });
})
.WithName("RetractProductToDraft")
.WithOpenApi()
.RequireAuthorization(VitrinAuthDefaults.MakerOrAdminPolicy);

app.MapPost("/api/products/{id}/archive", async (Guid id, HttpContext context, IMediator mediator) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var isAdmin = context.User.IsInRole("Admin");
    var result = await mediator.Send(new Vitrin.Product.Application.Commands.ArchiveProductCommand(id, userId.Value, isAdmin, context.User.GetUsername()), context.RequestAborted);
    if (result.IsFailure) return ApiProblemResults.BadRequest(result.Error, "product.archive_failed");

    return Results.Ok(new { Message = "Product archived." });
})
.WithName("ArchiveProduct")
.WithOpenApi()
.RequireAuthorization(VitrinAuthDefaults.MakerOrAdminPolicy);

app.MapPost("/api/products/{id:guid}/schedule", async (
    Guid id,
    [FromBody] ScheduleProductRequest request,
    HttpContext context,
    ProductDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var product = await db.Products
        .Include(item => item.TeamMembers)
        .FirstOrDefaultAsync(item => item.Id == id, context.RequestAborted);
    if (product is null) return Results.NotFound();
    if (!product.CanEdit(userId.Value) && !context.User.IsInRole("Admin")) return Results.Forbid();

    var result = product.SetScheduledLaunch(request.ScheduledLaunchAt);
    if (result.IsFailure) return ApiProblemResults.BadRequest(result.Error, "product.schedule_failed");

    await AddRevisionAsync(
        db,
        product,
        userId.Value,
        context.User.GetUsername(),
        request.ScheduledLaunchAt is null ? "schedule_cleared" : "schedule_changed",
        request.ScheduledLaunchAt is null ? "Scheduled launch cleared." : $"Launch scheduled for {product.ScheduledLaunchAt:O}.",
        context.RequestAborted);
    await db.SaveChangesAsync(context.RequestAborted);

    return Results.Ok(new { product.Id, product.Status, product.ScheduledLaunchAt });
})
.WithName("ScheduleProductLaunch")
.WithOpenApi()
.RequireAuthorization();

app.MapGet("/api/products/{id:guid}/revisions", async (Guid id, HttpContext context, ProductDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var access = await db.Products
        .AsNoTracking()
        .Where(item => item.Id == id)
        .Select(item => new
        {
            CanView = item.MakerId == userId.Value || item.TeamMembers.Any(member => member.UserId == userId.Value)
        })
        .FirstOrDefaultAsync(context.RequestAborted);
    if (access is null) return Results.NotFound();
    if (!access.CanView && !context.User.IsInRole("Admin")) return Results.Forbid();

    var revisions = await db.ProductRevisions
        .AsNoTracking()
        .Where(revision => revision.ProductId == id)
        .OrderByDescending(revision => revision.RevisionNumber)
        .Select(revision => new
        {
            revision.Id,
            revision.RevisionNumber,
            revision.ChangedByUserId,
            revision.ChangedByUsername,
            revision.ChangeType,
            revision.Summary,
            revision.Name,
            revision.Tagline,
            revision.Description,
            revision.ThumbnailUrl,
            revision.GalleryUrls,
            revision.Status,
            revision.ScheduledLaunchAt,
            revision.CreatedAt
        })
        .ToListAsync(context.RequestAborted);

    return Results.Ok(revisions);
})
.WithName("GetProductRevisions")
.WithOpenApi()
.RequireAuthorization();

app.MapGet("/api/products/{id:guid}/team", async (Guid id, HttpContext context, ProductDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var product = await db.Products
        .AsNoTracking()
        .Include(item => item.TeamMembers)
        .FirstOrDefaultAsync(item => item.Id == id, context.RequestAborted);
    if (product is null) return Results.NotFound();
    if (!product.CanViewManagement(userId.Value) && !context.User.IsInRole("Admin")) return Results.Forbid();

    return Results.Ok(new
    {
        OwnerUserId = product.MakerId,
        Members = product.TeamMembers
            .OrderBy(member => member.AddedAt)
            .Select(member => new { member.UserId, member.Role, member.AddedAt })
    });
})
.WithName("GetProductTeam")
.WithOpenApi()
.RequireAuthorization();

app.MapPost("/api/products/{id:guid}/team", async (
    Guid id,
    [FromBody] ProductTeamMemberRequest request,
    HttpContext context,
    ProductDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();
    if (!Enum.IsDefined(request.Role))
        return ApiProblemResults.BadRequest("Invalid team role.", "product.team.invalid_role");

    var product = await db.Products
        .Include(item => item.TeamMembers)
        .FirstOrDefaultAsync(item => item.Id == id, context.RequestAborted);
    if (product is null) return Results.NotFound();

    var result = product.AddOrUpdateTeamMember(userId.Value, request.MemberUserId, request.Role);
    if (result.IsFailure) return ApiProblemResults.BadRequest(result.Error, "product.team.update_failed");

    await AddRevisionAsync(
        db,
        product,
        userId.Value,
        context.User.GetUsername(),
        "team_changed",
        $"Team member {request.MemberUserId} set to {request.Role}.",
        context.RequestAborted);
    await db.SaveChangesAsync(context.RequestAborted);

    return Results.Ok(new { Message = "Product team updated." });
})
.WithName("AddOrUpdateProductTeamMember")
.WithOpenApi()
.RequireAuthorization();

app.MapDelete("/api/products/{id:guid}/team/{memberUserId:guid}", async (
    Guid id,
    Guid memberUserId,
    HttpContext context,
    ProductDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var product = await db.Products
        .Include(item => item.TeamMembers)
        .FirstOrDefaultAsync(item => item.Id == id, context.RequestAborted);
    if (product is null) return Results.NotFound();

    var result = product.RemoveTeamMember(userId.Value, memberUserId);
    if (result.IsFailure) return ApiProblemResults.BadRequest(result.Error, "product.team.remove_failed");

    await AddRevisionAsync(
        db,
        product,
        userId.Value,
        context.User.GetUsername(),
        "team_changed",
        $"Team member {memberUserId} removed.",
        context.RequestAborted);
    await db.SaveChangesAsync(context.RequestAborted);

    return Results.Ok(new { Message = "Team member removed." });
})
.WithName("RemoveProductTeamMember")
.WithOpenApi()
.RequireAuthorization();

app.MapPost("/api/products/{id:guid}/ownership/transfer", async (
    Guid id,
    [FromBody] TransferOwnershipRequest request,
    HttpContext context,
    ProductDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var product = await db.Products
        .Include(item => item.TeamMembers)
        .FirstOrDefaultAsync(item => item.Id == id, context.RequestAborted);
    if (product is null) return Results.NotFound();

    var result = product.TransferOwnership(userId.Value, request.NewOwnerUserId);
    if (result.IsFailure) return ApiProblemResults.BadRequest(result.Error, "product.ownership.transfer_failed");

    await AddRevisionAsync(
        db,
        product,
        userId.Value,
        context.User.GetUsername(),
        "ownership_transferred",
        $"Ownership transferred to {request.NewOwnerUserId}.",
        context.RequestAborted);
    await db.SaveChangesAsync(context.RequestAborted);

    return Results.Ok(new { Message = "Product ownership transferred.", OwnerUserId = product.MakerId });
})
.WithName("TransferProductOwnership")
.WithOpenApi()
.RequireAuthorization();

app.MapPost("/api/products/claims", async (
    [FromBody] CreateProductClaimRequest request,
    HttpContext context,
    ProductDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var slug = ExtractProductSlug(request.ProductSlugOrUrl);
    if (string.IsNullOrWhiteSpace(slug))
        return ApiProblemResults.BadRequest("A valid product slug or URL is required.", "product.claim.invalid_slug");

    var product = await db.Products
        .AsNoTracking()
        .Where(item => item.Slug == slug)
        .Select(item => new
        {
            item.Id,
            item.MakerId,
            IsMember = item.TeamMembers.Any(member => member.UserId == userId.Value)
        })
        .FirstOrDefaultAsync(context.RequestAborted);
    if (product is null) return Results.NotFound(new { Message = "Product not found." });
    if (product.MakerId == userId.Value)
        return ApiProblemResults.BadRequest("You already own this product.", "product.claim.already_owner");
    if (product.IsMember)
        return ApiProblemResults.BadRequest("You are already on this product team.", "product.claim.already_member");

    var hasPendingClaim = await db.ProductClaimRequests.AnyAsync(
        claim => claim.ProductId == product.Id &&
                 claim.ClaimantUserId == userId.Value &&
                 claim.Status == ProductClaimStatus.Pending,
        context.RequestAborted);
    if (hasPendingClaim)
        return ApiProblemResults.Conflict("You already have a pending ownership claim.", "product.claim.already_pending");

    var claimResult = ProductClaimRequest.Create(
        product.Id,
        userId.Value,
        context.User.GetUsername(),
        request.Message);
    if (claimResult.IsFailure)
        return ApiProblemResults.BadRequest(claimResult.Error, "product.claim.create_failed");

    db.ProductClaimRequests.Add(claimResult.Value!);
    await db.SaveChangesAsync(context.RequestAborted);
    return Results.Ok(new { ClaimId = claimResult.Value!.Id, Message = "Ownership claim submitted for admin review." });
})
.WithName("CreateProductOwnershipClaim")
.WithOpenApi()
.RequireAuthorization();

app.MapGet("/api/products/my-claims", async (HttpContext context, ProductDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var claims = await db.ProductClaimRequests
        .AsNoTracking()
        .Where(claim => claim.ClaimantUserId == userId.Value)
        .Join(
            db.Products.AsNoTracking(),
            claim => claim.ProductId,
            product => product.Id,
            (claim, product) => new
            {
                claim.Id,
                claim.ProductId,
                ProductName = product.Name,
                ProductSlug = product.Slug,
                claim.Message,
                claim.Status,
                claim.ReviewNote,
                claim.CreatedAt,
                claim.ReviewedAt
            })
        .OrderByDescending(claim => claim.CreatedAt)
        .ToListAsync(context.RequestAborted);

    return Results.Ok(claims);
})
.WithName("GetMyProductOwnershipClaims")
.WithOpenApi()
.RequireAuthorization();

app.MapGet("/api/products/admin/claims", async (string? status, ProductDbContext db, HttpContext context) =>
{
    var query = db.ProductClaimRequests.AsNoTracking();
    if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ProductClaimStatus>(status, true, out var parsedStatus))
        query = query.Where(claim => claim.Status == parsedStatus);

    var claims = await query
        .Join(
            db.Products.AsNoTracking(),
            claim => claim.ProductId,
            product => product.Id,
            (claim, product) => new
            {
                claim.Id,
                claim.ProductId,
                ProductName = product.Name,
                ProductSlug = product.Slug,
                CurrentOwnerUserId = product.MakerId,
                claim.ClaimantUserId,
                claim.ClaimantUsername,
                claim.Message,
                claim.Status,
                claim.ReviewNote,
                claim.CreatedAt,
                claim.ReviewedAt,
                claim.ReviewedByUserId
            })
        .OrderByDescending(claim => claim.CreatedAt)
        .Take(200)
        .ToListAsync(context.RequestAborted);

    return Results.Ok(claims);
})
.WithName("AdminGetProductOwnershipClaims")
.WithOpenApi()
.RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

app.MapPost("/api/products/admin/claims/{claimId:guid}/decision", async (
    Guid claimId,
    [FromBody] ProductClaimDecisionRequest request,
    HttpContext context,
    ProductDbContext db,
    ProductEventPublisher eventPublisher,
    IAuditLogger auditLogger) =>
{
    var adminUserId = context.User.GetUserId();
    if (adminUserId is null) return Results.Unauthorized();

    var claim = await db.ProductClaimRequests.FirstOrDefaultAsync(item => item.Id == claimId, context.RequestAborted);
    if (claim is null) return Results.NotFound();

    var product = await db.Products
        .Include(item => item.TeamMembers)
        .FirstOrDefaultAsync(item => item.Id == claim.ProductId, context.RequestAborted);
    if (product is null) return Results.NotFound();

    var decisionResult = request.Approved
        ? claim.Approve(adminUserId.Value, request.Note)
        : claim.Reject(adminUserId.Value, request.Note);
    if (decisionResult.IsFailure)
        return ApiProblemResults.BadRequest(decisionResult.Error, "product.claim.decision_failed");

    if (request.Approved)
    {
        var currentOwnerId = product.MakerId;
        var memberResult = product.AddOrUpdateTeamMember(currentOwnerId, claim.ClaimantUserId, ProductTeamRole.Editor);
        if (memberResult.IsFailure)
            return ApiProblemResults.BadRequest(memberResult.Error, "product.claim.transfer_failed");

        var transferResult = product.TransferOwnership(currentOwnerId, claim.ClaimantUserId);
        if (transferResult.IsFailure)
            return ApiProblemResults.BadRequest(transferResult.Error, "product.claim.transfer_failed");

        await AddRevisionAsync(
            db,
            product,
            adminUserId.Value,
            context.User.GetUsername(),
            "ownership_claim_approved",
            $"Ownership transferred from {currentOwnerId} to {claim.ClaimantUserId} after admin approval.",
            context.RequestAborted);
    }

    eventPublisher.EnqueueOwnershipNotification(
        claim.ClaimantUserId,
        product.Name,
        product.Id,
        request.Approved,
        request.Note);
    await db.SaveChangesAsync(context.RequestAborted);

    await auditLogger.WriteAsync(
        new AuditEvent(
            request.Approved ? "admin.product_claim_approved" : "admin.product_claim_rejected",
            adminUserId.Value,
            "ProductClaim",
            claimId.ToString(),
            "Succeeded",
            context.TraceIdentifier),
        context.RequestAborted);

    return Results.Ok(new { Message = request.Approved ? "Ownership claim approved." : "Ownership claim rejected." });
})
.WithName("AdminDecideProductOwnershipClaim")
.WithOpenApi()
.RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

// Admin: list all products by status (Draft, Rejected, Archived too)
app.MapGet("/api/products/admin/all", async (string? status, int? page, ProductDbContext db) =>
{
    var pageNumber = Math.Max(1, page ?? 1);
    const int pageSize = 50;

    var query = db.Products.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Vitrin.Product.Domain.Entities.ProductStatus>(status, true, out var parsedStatus))
    {
        query = query.Where(p => p.Status == parsedStatus);
    }

    var total = await query.CountAsync();
    var products = await query
        .OrderByDescending(p => p.CreatedAt)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .Select(p => new {
            p.Id, p.Name, p.Slug, p.Tagline, p.ThumbnailUrl,
            p.MakerId, p.Status, p.RejectionReason,
            p.CreatedAt, p.PublishedAt, p.ArchivedAt, p.ScheduledLaunchAt
        })
        .ToListAsync();

    return Results.Ok(new { Total = total, Page = pageNumber, PageSize = pageSize, Items = products });
})
.WithName("AdminGetAllProducts")
.WithOpenApi()
.RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

app.MapGet("/api/products/search", async (
    string q,
    int? limit,
    HttpContext context,
    ProductDbContext db,
    ProductEventPublisher eventPublisher) =>
{
    if (string.IsNullOrWhiteSpace(q)) return Results.Ok(new List<object>());

    var requestedLimit = Math.Clamp(limit ?? 20, 1, 50);
    var term = q.Trim()[..Math.Min(q.Trim().Length, 100)];
    var escapedTerm = EscapeLikePattern(term);
    var containsPattern = $"%{escapedTerm}%";
    var prefixPattern = $"{escapedTerm}%";
    var similarityThreshold = term.Length <= 3 ? 0.12 : 0.20;

    var matches = await db.Products
        .AsNoTracking()
        .Where(p => p.Status == Vitrin.Product.Domain.Entities.ProductStatus.Published)
        .Where(product =>
            EF.Property<NpgsqlTsVector>(product, "SearchVector")
                .Matches(EF.Functions.WebSearchToTsQuery("simple", term)) ||
            EF.Functions.TrigramsSimilarity(product.Name, term) >= similarityThreshold ||
            EF.Functions.TrigramsSimilarity(product.Tagline, term) >= similarityThreshold ||
            EF.Functions.TrigramsSimilarity(product.Description, term) >= similarityThreshold ||
            EF.Functions.ILike(product.Name, containsPattern, "\\") ||
            EF.Functions.ILike(product.Tagline, containsPattern, "\\") ||
            product.Topics.Any(topic =>
                EF.Functions.ILike(topic.Name, containsPattern, "\\") ||
                EF.Functions.TrigramsSimilarity(topic.Name, term) >= similarityThreshold))
        .Select(product => new
        {
            product.Id,
            ExactName = EF.Functions.ILike(product.Name, escapedTerm, "\\"),
            NamePrefix = EF.Functions.ILike(product.Name, prefixPattern, "\\"),
            FullTextRank = EF.Property<NpgsqlTsVector>(product, "SearchVector")
                .Rank(EF.Functions.WebSearchToTsQuery("simple", term)),
            NameSimilarity = EF.Functions.TrigramsSimilarity(product.Name, term),
            TaglineSimilarity = EF.Functions.TrigramsSimilarity(product.Tagline, term),
            DescriptionSimilarity = EF.Functions.TrigramsSimilarity(product.Description, term),
            TopicMatch = product.Topics.Any(topic =>
                EF.Functions.ILike(topic.Name, containsPattern, "\\") ||
                EF.Functions.TrigramsSimilarity(topic.Name, term) >= similarityThreshold)
        })
        .OrderByDescending(match => match.ExactName)
        .ThenByDescending(match => match.NamePrefix)
        .ThenByDescending(match => match.FullTextRank)
        .ThenByDescending(match => match.NameSimilarity)
        .Take(requestedLimit)
        .ToListAsync(context.RequestAborted);

    var ids = matches.Select(match => match.Id).ToList();
    var productsById = await db.Products
        .AsNoTracking()
        .Where(product => ids.Contains(product.Id))
        .ProjectToResponse()
        .ToDictionaryAsync(product => product.Id, context.RequestAborted);

    var now = DateTime.UtcNow;
    var products = matches
        .Where(match => productsById.ContainsKey(match.Id))
        .Select(match =>
        {
            var product = productsById[match.Id];
            var searchScore =
                (match.ExactName ? 6d : 0d) +
                (match.NamePrefix ? 3d : 0d) +
                (match.FullTextRank * 4d) +
                (match.NameSimilarity * 3d) +
                (match.TaglineSimilarity * 2d) +
                match.DescriptionSimilarity +
                (match.TopicMatch ? 1.5d : 0d);
            var matchType = match.ExactName
                ? "exact"
                : match.NamePrefix
                    ? "prefix"
                    : match.FullTextRank > 0
                        ? "full_text"
                        : match.TopicMatch
                            ? "topic"
                            : "typo";

            return product with
            {
                SearchScore = Math.Round(searchScore, 3),
                MatchType = matchType,
                TrendScore = CalculateTrendScore(product, now)
            };
        })
        .ToList();

    eventPublisher.EnqueueSearchPerformed(term, products.Count, context.User.GetUserId());
    await db.SaveChangesAsync(context.RequestAborted);

    return Results.Ok(products);
})
.WithName("SearchProducts")
.WithOpenApi();

// --- COLLECTIONS ---

app.MapGet("/api/collections", async (HttpContext context, ProductDbContext db) =>
{
    var requesterId = context.User.GetUserId();
    var currentUserId = requesterId ?? Guid.Empty;
    var query = db.Collections.AsNoTracking().AsQueryable();
    query = requesterId is null
        ? query.Where(collection => collection.Visibility == CollectionVisibility.Public)
        : query.Where(collection =>
            collection.Visibility == CollectionVisibility.Public ||
            collection.UserId == currentUserId ||
            collection.Collaborators.Any(member => member.UserId == currentUserId));

    var collections = await query
        .OrderByDescending(collection => collection.CreatedAt)
        .Take(100)
        .Select(collection => new CollectionSummaryResponse(
            collection.Id,
            collection.Name,
            collection.Slug,
            collection.Description,
            collection.UserId,
            collection.Visibility,
            collection.CreatedAt,
            collection.Products.Count,
            collection.Collaborators.Count,
            collection.UserId == currentUserId,
            collection.UserId == currentUserId || collection.Collaborators.Any(member =>
                member.UserId == currentUserId && member.Role == CollectionCollaboratorRole.Editor)))
        .ToListAsync(context.RequestAborted);
    return Results.Ok(collections);
});

app.MapGet("/api/collections/user/{userId}", async (Guid userId, HttpContext context, ProductDbContext db) =>
{
    var requesterId = context.User.GetUserId();
    var currentUserId = requesterId ?? Guid.Empty;
    var query = db.Collections
        .AsNoTracking()
        .Where(collection => collection.UserId == userId);
    query = requesterId is null
        ? query.Where(collection => collection.Visibility == CollectionVisibility.Public)
        : query.Where(collection =>
            collection.Visibility == CollectionVisibility.Public ||
            collection.UserId == currentUserId ||
            collection.Collaborators.Any(member => member.UserId == currentUserId));

    var collections = await query
        .OrderByDescending(collection => collection.CreatedAt)
        .Take(100)
        .Select(collection => new CollectionSummaryResponse(
            collection.Id,
            collection.Name,
            collection.Slug,
            collection.Description,
            collection.UserId,
            collection.Visibility,
            collection.CreatedAt,
            collection.Products.Count,
            collection.Collaborators.Count,
            collection.UserId == currentUserId,
            collection.UserId == currentUserId || collection.Collaborators.Any(member =>
                member.UserId == currentUserId && member.Role == CollectionCollaboratorRole.Editor)))
        .ToListAsync(context.RequestAborted);
    return Results.Ok(collections);
});

app.MapGet("/api/collections/me", async (HttpContext context, ProductDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var collections = await db.Collections
        .AsNoTracking()
        .Where(collection =>
            collection.UserId == userId.Value ||
            collection.Collaborators.Any(member => member.UserId == userId.Value))
        .OrderByDescending(collection => collection.CreatedAt)
        .Take(100)
        .Select(collection => new CollectionSummaryResponse(
            collection.Id,
            collection.Name,
            collection.Slug,
            collection.Description,
            collection.UserId,
            collection.Visibility,
            collection.CreatedAt,
            collection.Products.Count,
            collection.Collaborators.Count,
            collection.UserId == userId.Value,
            collection.UserId == userId.Value || collection.Collaborators.Any(member =>
                member.UserId == userId.Value && member.Role == CollectionCollaboratorRole.Editor)))
        .ToListAsync(context.RequestAborted);

    return Results.Ok(collections);
}).RequireAuthorization();

app.MapGet("/api/collections/by-slug/{slug}", async (string slug, HttpContext context, ProductDbContext db) =>
{
    var requesterId = context.User.GetUserId();
    var currentUserId = requesterId ?? Guid.Empty;
    var query = db.Collections
        .AsNoTracking()
        .Where(collection => collection.Slug == slug);
    query = requesterId is null
        ? query.Where(collection => collection.Visibility == CollectionVisibility.Public)
        : query.Where(collection =>
            collection.Visibility == CollectionVisibility.Public ||
            collection.UserId == currentUserId ||
            collection.Collaborators.Any(member => member.UserId == currentUserId));

    var collection = await query
        .ProjectToDetailsResponse()
        .FirstOrDefaultAsync(context.RequestAborted);
        
    if (collection == null) return Results.NotFound();

    return Results.Ok(collection with
    {
        IsOwner = requesterId is not null && collection.UserId == currentUserId,
        CanEdit = requesterId is not null &&
            (collection.UserId == currentUserId || collection.Collaborators.Any(member =>
                member.UserId == currentUserId && member.Role == CollectionCollaboratorRole.Editor))
    });
});

app.MapPost("/api/collections", async (HttpContext context, [Microsoft.AspNetCore.Mvc.FromBody] CreateCollectionRequest request, ProductDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var baseSlug = SlugGenerator.Generate(request.Name);
    if (string.IsNullOrEmpty(baseSlug))
    {
        return ApiProblemResults.BadRequest("Collection name must produce a valid slug.", "collection.invalid_name");
    }

    // Avoid a check-then-insert race. The database unique constraint remains authoritative.
    var suffix = Guid.NewGuid().ToString("N")[..8];
    var slugPrefix = baseSlug[..Math.Min(baseSlug.Length, 91)];
    var slug = $"{slugPrefix}-{suffix}";

    if (!Enum.IsDefined(request.Visibility))
    {
        return ApiProblemResults.BadRequest("Collection visibility is invalid.", "collection.invalid_visibility");
    }

    var collection = Collection.Create(userId.Value, request.Name, slug, request.Description, request.Visibility);
    db.Collections.Add(collection);
    try
    {
        await db.SaveChangesAsync();
    }
    catch (DbUpdateException exception) when (
        ProductDatabaseErrors.TryGetUniqueConstraint(exception, out var constraint) &&
        constraint == ProductDatabaseConstraints.CollectionSlug)
    {
        return ApiProblemResults.Conflict(
            "Collection slug collision. Please retry.",
            "collection.slug_conflict");
    }
    
    return Results.Ok(new CollectionSummaryResponse(
        collection.Id,
        collection.Name,
        collection.Slug,
        collection.Description,
        collection.UserId,
        collection.Visibility,
        collection.CreatedAt,
        0,
        0,
        true,
        true));
}).RequireAuthorization();

app.MapPatch("/api/collections/{id}/visibility", async (
    Guid id,
    HttpContext context,
    [FromBody] UpdateCollectionVisibilityRequest request,
    ProductDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();
    if (!Enum.IsDefined(request.Visibility))
        return ApiProblemResults.BadRequest("Collection visibility is invalid.", "collection.invalid_visibility");

    var collection = await db.Collections.FindAsync([id], context.RequestAborted);
    if (collection is null) return Results.NotFound();
    if (collection.UserId != userId.Value) return Results.Forbid();

    collection.SetVisibility(request.Visibility);
    await db.SaveChangesAsync(context.RequestAborted);
    return Results.Ok(new { collection.Id, collection.Visibility });
}).RequireAuthorization();

app.MapPost("/api/collections/{id}/collaborators", async (
    Guid id,
    HttpContext context,
    [FromBody] CollectionCollaboratorRequest request,
    ProductDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();
    if (!Enum.IsDefined(request.Role))
        return ApiProblemResults.BadRequest("Collaborator role is invalid.", "collection.invalid_collaborator_role");

    var collection = await db.Collections
        .Include(item => item.Collaborators)
        .FirstOrDefaultAsync(item => item.Id == id, context.RequestAborted);
    if (collection is null) return Results.NotFound();
    if (collection.UserId != userId.Value) return Results.Forbid();
    if (request.UserId == collection.UserId)
        return ApiProblemResults.BadRequest("The owner is already part of this collection.", "collection.owner_cannot_collaborate");

    collection.AddOrUpdateCollaborator(request.UserId, request.Role);
    await db.SaveChangesAsync(context.RequestAborted);
    return Results.Ok(new { request.UserId, request.Role });
}).RequireAuthorization();

app.MapDelete("/api/collections/{id}/collaborators/{collaboratorUserId}", async (
    Guid id,
    Guid collaboratorUserId,
    HttpContext context,
    ProductDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var collection = await db.Collections
        .Include(item => item.Collaborators)
        .FirstOrDefaultAsync(item => item.Id == id, context.RequestAborted);
    if (collection is null) return Results.NotFound();
    if (collection.UserId != userId.Value) return Results.Forbid();

    collection.RemoveCollaborator(collaboratorUserId);
    await db.SaveChangesAsync(context.RequestAborted);
    return Results.NoContent();
}).RequireAuthorization();

app.MapPost("/api/collections/{id}/products/{productId}", async (Guid id, Guid productId, HttpContext context, ProductDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var collection = await db.Collections
        .Include(c => c.Products)
        .Include(c => c.Collaborators)
        .FirstOrDefaultAsync(c => c.Id == id, context.RequestAborted);
    if (collection == null) return Results.NotFound("Collection not found");
    if (!collection.CanEdit(userId.Value)) return Results.Forbid();
    
    var product = await db.Products.FindAsync([productId], context.RequestAborted);
    if (product == null) return Results.NotFound("Product not found");
    
    collection.AddProduct(product);
    await db.SaveChangesAsync(context.RequestAborted);
    
    return Results.Ok(new { Message = "Product added to collection" });
}).RequireAuthorization();

app.MapDelete("/api/collections/{id}/products/{productId}", async (Guid id, Guid productId, HttpContext context, ProductDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var collection = await db.Collections
        .Include(c => c.Products)
        .Include(c => c.Collaborators)
        .FirstOrDefaultAsync(c => c.Id == id, context.RequestAborted);
    if (collection == null) return Results.NotFound("Collection not found");
    if (!collection.CanEdit(userId.Value)) return Results.Forbid();
    
    collection.RemoveProduct(productId);
    await db.SaveChangesAsync(context.RequestAborted);
    
    return Results.Ok(new { Message = "Product removed from collection" });
}).RequireAuthorization();

app.Run();

static double CalculateTrendScore(ProductResponse product, DateTime utcNow)
{
    var publishedAt = product.PublishedAt ?? product.CreatedAt;
    return ProductTrendScore.Calculate(
        product.Upvotes,
        product.CommentCount,
        product.ViewCount,
        publishedAt,
        utcNow);
}

static bool IsAfterCursor(ProductResponse product, double value, SortedKeysetCursor cursor)
{
    if (value < cursor.Value) return true;
    if (value > cursor.Value) return false;

    var publishedAt = product.PublishedAt ?? product.CreatedAt;
    return publishedAt < cursor.TimestampUtc ||
        (publishedAt == cursor.TimestampUtc && product.Id.CompareTo(cursor.Id) < 0);
}

static double GetSortValue(ProductResponse product, string sort) => sort switch
{
    "trending" => product.TrendScore,
    "most_voted" => product.Upvotes,
    "most_commented" => product.CommentCount,
    "most_viewed" => product.ViewCount,
    _ => 0
};

static string CreateProductFilterScope(
    string? searchTerm,
    IReadOnlyCollection<string> topicSlugs,
    int? minUpvotes,
    int? minComments,
    int? minViews,
    DateTime? publishedFrom,
    DateTime? publishedTo)
{
    var normalized = string.Join('|',
        searchTerm?.Trim().ToLowerInvariant() ?? string.Empty,
        string.Join(',', topicSlugs.OrderBy(value => value, StringComparer.Ordinal)),
        minUpvotes?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
        minComments?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
        minViews?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
        publishedFrom?.ToString("O", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
        publishedTo?.ToString("O", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
    var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(normalized));
    return Convert.ToHexString(hash)[..24];
}

static async Task AddRevisionAsync(
    ProductDbContext db,
    ProductItem product,
    Guid changedByUserId,
    string changedByUsername,
    string changeType,
    string? summary,
    CancellationToken cancellationToken)
{
    var previousRevisionNumber = await db.ProductRevisions
        .Where(revision => revision.ProductId == product.Id)
        .MaxAsync(revision => (int?)revision.RevisionNumber, cancellationToken) ?? 0;

    db.ProductRevisions.Add(ProductRevision.Create(
        product,
        previousRevisionNumber + 1,
        changedByUserId,
        changedByUsername,
        changeType,
        summary));
}

static string ExtractProductSlug(string slugOrUrl)
{
    var value = slugOrUrl?.Trim() ?? string.Empty;
    if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        value = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;

    return value.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;
}

static string EscapeLikePattern(string value) => value
    .Replace("\\", "\\\\", StringComparison.Ordinal)
    .Replace("%", "\\%", StringComparison.Ordinal)
    .Replace("_", "\\_", StringComparison.Ordinal);

public record CreateProductRequest(
    string Name,
    string Tagline,
    string Description,
    string Slug,
    List<string> Topics,
    string? ThumbnailUrl,
    List<string>? GalleryUrls,
    bool SaveAsDraft = false);

public record RejectProductRequest(string? Reason);

public record ScheduleProductRequest(DateTime? ScheduledLaunchAt);

public record ProductTeamMemberRequest(Guid MemberUserId, ProductTeamRole Role);

public record TransferOwnershipRequest(Guid NewOwnerUserId);

public record CreateProductClaimRequest(string ProductSlugOrUrl, string Message);

public record ProductClaimDecisionRequest(bool Approved, string? Note);

public record MyProductResponse(
    Guid Id,
    string Name,
    string Slug,
    string Tagline,
    string ThumbnailUrl,
    Vitrin.Product.Domain.Entities.ProductStatus Status,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime? PublishedAt,
    DateTime? ArchivedAt,
    DateTime? ScheduledLaunchAt,
    int Upvotes,
    bool IsOwner,
    ProductTeamRole? TeamRole);

public record CreateCollectionRequest(
    string Name,
    string Description,
    CollectionVisibility Visibility = CollectionVisibility.Public);

public record UpdateCollectionVisibilityRequest(CollectionVisibility Visibility);

public record CollectionCollaboratorRequest(Guid UserId, CollectionCollaboratorRole Role);
