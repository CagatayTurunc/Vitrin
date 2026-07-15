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
using Vitrin.Product.Infrastructure.Repositories;

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
        request.GalleryUrls);
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

    KeysetCursor? decodedCursor = null;
    if (cursor is not null)
    {
        if (!KeysetCursorCodec.TryDecode(cursor, out var parsedCursor))
        {
            return ApiProblemResults.BadRequest("The pagination cursor is invalid.", "pagination.invalid_cursor");
        }

        decodedCursor = parsedCursor;
    }

    var query = db.Products
        .AsNoTracking()
        .Where(product => product.Status == Vitrin.Product.Domain.Entities.ProductStatus.Published);

    if (!string.IsNullOrEmpty(topicSlug))
    {
        query = query.Where(product => product.Topics.Any(topic => topic.Slug == topicSlug));
    }

    if (decodedCursor is { } keyset)
    {
        query = query.Where(product =>
            product.PublishedAt < keyset.TimestampUtc ||
            (product.PublishedAt == keyset.TimestampUtc && product.Id.CompareTo(keyset.Id) < 0));
    }

    var rows = await query
        .OrderByDescending(product => product.PublishedAt)
        .ThenByDescending(product => product.Id)
        .Take(requestedPageSize + 1)
        .ProjectToResponse()
        .ToListAsync(context.RequestAborted);
    var hasMore = rows.Count > requestedPageSize;
    var items = rows.Take(requestedPageSize).ToList();
    var lastItem = items.LastOrDefault();
    var nextCursor = hasMore && lastItem?.PublishedAt is { } publishedAt
        ? KeysetCursorCodec.Encode(publishedAt, lastItem.Id)
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

app.MapGet("/api/products/{slug}", async (string slug, ProductDbContext db) =>
{
    var product = await db.Products
        .AsNoTracking()
        .Where(p => p.Status == Vitrin.Product.Domain.Entities.ProductStatus.Published && p.Slug == slug)
        .ProjectToResponse()
        .FirstOrDefaultAsync();

    if (product == null) return Results.NotFound();
    
    return Results.Ok(product);
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
        .Where(p => p.MakerId == makerId)
        .OrderByDescending(product => product.CreatedAt)
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
            p.Id, p.Name, p.Slug, p.Tagline, p.Description, p.ThumbnailUrl, p.GalleryUrls, p.MakerId, p.Status, p.CreatedAt, p.PublishedAt
        })
        .ToListAsync();
    return Results.Ok(products);
})
.WithName("GetPendingProducts")
.WithOpenApi()
.RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

app.MapPost("/api/products/admin/{id}/approve", async (Guid id, HttpContext context, ProductDbContext db, Vitrin.Product.Infrastructure.Kafka.ProductEventPublisher eventPublisher, IAuditLogger auditLogger) =>
{
    var product = await db.Products.FindAsync(id);
    if (product == null) return Results.NotFound();
    
    var result = product.Approve();
    if (result.IsFailure) return ApiProblemResults.BadRequest(result.Error, "product.approve_failed");
    
    // Product state and its integration event are committed in one transaction.
    await eventPublisher.PublishProductPublished(new Vitrin.Shared.Contracts.Events.ProductPublishedEvent
    {
        ProductId   = product.Id,
        MakerId     = product.MakerId,
        ProductName = product.Name,
        ProductSlug = product.Slug
    }, context.RequestAborted);

    await auditLogger.WriteAsync(
        new AuditEvent("admin.product_approved", context.User.GetUserId(), "Product", id.ToString(), "Succeeded", context.TraceIdentifier),
        context.RequestAborted);

    return Results.Ok(new { Message = "Product approved successfully!" });
})
.WithName("ApproveProduct")
.WithOpenApi()
.RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

app.MapPost("/api/products/admin/{id}/reject", async (Guid id, HttpContext context, ProductDbContext db, IAuditLogger auditLogger) =>
{
    var product = await db.Products.FindAsync(id);
    if (product == null) return Results.NotFound();
    
    var result = product.Reject();
    if (result.IsFailure) return ApiProblemResults.BadRequest(result.Error, "product.reject_failed");
    
    await db.SaveChangesAsync();
    await auditLogger.WriteAsync(
        new AuditEvent("admin.product_rejected", context.User.GetUserId(), "Product", id.ToString(), "Succeeded", context.TraceIdentifier),
        context.RequestAborted);
    return Results.Ok(new { Message = "Product rejected successfully!" });
})
.WithName("RejectProduct")
.WithOpenApi()
.RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

app.MapGet("/api/products/search", async (string q, int? limit, ProductDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(q)) return Results.Ok(new List<object>());

    var requestedLimit = Math.Clamp(limit ?? 20, 1, 50);
    var term = q.Trim();
    var escapedTerm = EscapeLikePattern(term);
    var containsPattern = $"%{escapedTerm}%";
    var prefixPattern = $"{escapedTerm}%";

    var query = db.Products
        .AsNoTracking()
        .Where(p => p.Status == Vitrin.Product.Domain.Entities.ProductStatus.Published)
        .Where(p => EF.Functions.ILike(p.Name, containsPattern, "\\") ||
                    EF.Functions.ILike(p.Tagline, containsPattern, "\\") ||
                    EF.Functions.ILike(p.Description, containsPattern, "\\") ||
                    p.Topics.Any(t => EF.Functions.ILike(t.Name, containsPattern, "\\")));

    var products = await query
        .OrderByDescending(product => EF.Functions.ILike(product.Name, escapedTerm, "\\"))
        .ThenByDescending(product => EF.Functions.ILike(product.Name, prefixPattern, "\\"))
        .ThenByDescending(product => product.PublishedAt)
        .Take(requestedLimit)
        .ProjectToResponse()
        .ToListAsync();
        
    return Results.Ok(products);
})
.WithName("SearchProducts")
.WithOpenApi();

// --- COLLECTIONS ---

app.MapGet("/api/collections", async (ProductDbContext db) =>
{
    var collections = await db.Collections
        .AsNoTracking()
        .OrderByDescending(c => c.CreatedAt)
        .Take(100)
        .Select(c => new {
            c.Id, c.Name, c.Slug, c.Description, c.UserId, c.CreatedAt, ProductCount = c.Products.Count
        })
        .ToListAsync();
    return Results.Ok(collections);
});

app.MapGet("/api/collections/user/{userId}", async (Guid userId, ProductDbContext db) =>
{
    var collections = await db.Collections
        .AsNoTracking()
        .Where(c => c.UserId == userId)
        .OrderByDescending(c => c.CreatedAt)
        .Take(100)
        .Select(c => new {
            c.Id, c.Name, c.Slug, c.Description, c.UserId, c.CreatedAt, ProductCount = c.Products.Count
        })
        .ToListAsync();
    return Results.Ok(collections);
});

app.MapGet("/api/collections/me", async (HttpContext context, ProductDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var collections = await db.Collections
        .AsNoTracking()
        .Where(c => c.UserId == userId.Value)
        .OrderByDescending(c => c.CreatedAt)
        .Take(100)
        .Select(c => new {
            c.Id, c.Name, c.Slug, c.Description, c.UserId, c.CreatedAt, ProductCount = c.Products.Count
        })
        .ToListAsync();

    return Results.Ok(collections);
}).RequireAuthorization();

app.MapGet("/api/collections/by-slug/{slug}", async (string slug, ProductDbContext db) =>
{
    var collection = await db.Collections
        .AsNoTracking()
        .Where(c => c.Slug == slug)
        .ProjectToDetailsResponse()
        .FirstOrDefaultAsync();
        
    if (collection == null) return Results.NotFound();
    
    return Results.Ok(collection);
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

    var collection = Vitrin.Product.Domain.Entities.Collection.Create(userId.Value, request.Name, slug, request.Description);
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
    
    return Results.Ok(collection);
}).RequireAuthorization();

app.MapPost("/api/collections/{id}/products/{productId}", async (Guid id, Guid productId, HttpContext context, ProductDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var collection = await db.Collections.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
    if (collection == null) return Results.NotFound("Collection not found");
    if (collection.UserId != userId.Value) return Results.Forbid();
    
    var product = await db.Products.FindAsync(productId);
    if (product == null) return Results.NotFound("Product not found");
    
    collection.AddProduct(product);
    await db.SaveChangesAsync();
    
    return Results.Ok(new { Message = "Product added to collection" });
}).RequireAuthorization();

app.MapDelete("/api/collections/{id}/products/{productId}", async (Guid id, Guid productId, HttpContext context, ProductDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var collection = await db.Collections.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
    if (collection == null) return Results.NotFound("Collection not found");
    if (collection.UserId != userId.Value) return Results.Forbid();
    
    collection.RemoveProduct(productId);
    await db.SaveChangesAsync();
    
    return Results.Ok(new { Message = "Product removed from collection" });
}).RequireAuthorization();

app.Run();

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
    List<string>? GalleryUrls);

public record CreateCollectionRequest(string Name, string Description);
