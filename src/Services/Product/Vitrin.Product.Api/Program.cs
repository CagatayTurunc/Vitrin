using Microsoft.EntityFrameworkCore;
using Vitrin.Product.Application.Commands;
using Vitrin.Product.Infrastructure;
using Vitrin.Product.Infrastructure.Data;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vitrin.Shared.Infrastructure.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// JWT doğrulama yardımcısı (imza doğrulamalı)
builder.Services.AddSingleton<JwtTokenValidator>();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateProductCommand).Assembly));

// Infrastructure: DbContext + Repository + Kafka Producer + Kafka Consumer (VotingEventsConsumer)
builder.Services.AddProductInfrastructure(builder.Configuration);

var app = builder.Build();

// Migrate Database on startup for ease of development
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");

app.MapPost("/api/products/debug", ([FromBody] CreateProductCommand command) =>
{
    return Results.Ok(command);
});

app.MapPost("/api/products", async ([FromBody] CreateProductCommand command, IMediator mediator) =>
{
    var result = await mediator.Send(command);
    if (result.IsSuccess)
    {
        return Results.Ok(new { ProductId = result.Value, Message = "Product created successfully!" });
    }
    return Results.BadRequest(new { Error = result.Error });
})
.WithName("CreateProduct")
.WithOpenApi();

app.MapGet("/api/products", async (string? topicSlug, ProductDbContext db) =>
{
    // ProductStatus.Published is 2
    var query = db.Products
        .Include(p => p.Topics)
        .Include(p => p.Upvotes)
        .Where(p => p.Status == Vitrin.Product.Domain.Entities.ProductStatus.Published);

    if (!string.IsNullOrEmpty(topicSlug))
    {
        query = query.Where(p => p.Topics.Any(t => t.Slug == topicSlug));
    }

    var products = await query
        .Select(p => new {
            p.Id, p.Name, p.Slug, p.Tagline, p.Description, p.ThumbnailUrl, p.GalleryUrls, p.MakerId, p.Status, p.CreatedAt, p.PublishedAt, p.Topics,
            Upvotes = p.Upvotes.Count
        })
        .ToListAsync();
    return Results.Ok(products);
})
.WithName("GetProducts")
.WithOpenApi();

app.MapGet("/api/topics", async (ProductDbContext db) =>
{
    var topics = await db.Topics.ToListAsync();
    return Results.Ok(topics);
})
.WithName("GetTopics")
.WithOpenApi();

app.MapGet("/api/products/{slug}", async (string slug, ProductDbContext db) =>
{
    var product = await db.Products
        .Include(p => p.Topics)
        .Include(p => p.Upvotes)
        .Where(p => p.Status == Vitrin.Product.Domain.Entities.ProductStatus.Published && p.Slug == slug)
        .Select(p => new {
            p.Id, p.Name, p.Slug, p.Tagline, p.Description, p.ThumbnailUrl, p.GalleryUrls, p.MakerId, p.Status, p.CreatedAt, p.PublishedAt, p.Topics,
            Upvotes = p.Upvotes.Count
        })
        .FirstOrDefaultAsync();

    if (product == null) return Results.NotFound();
    
    return Results.Ok(product);
})
.WithName("GetProductBySlug")
.WithOpenApi();

app.MapGet("/api/products/batch", async (string ids, ProductDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(ids)) return Results.Ok(new List<object>());
    
    var idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(idStr => Guid.TryParse(idStr, out var g) ? g : Guid.Empty)
                    .Where(g => g != Guid.Empty)
                    .ToList();
                    
    var products = await db.Products
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

app.MapPost("/api/products/{id}/vote", async (Guid id, HttpContext context, JwtTokenValidator jwt, IMediator mediator) =>
{
    var userId = jwt.GetUserId(context);
    if (userId == null) return Results.Unauthorized();

    var result = await mediator.Send(new ToggleUpvoteCommand(id, userId.Value));
    if (result.IsSuccess)
    {
        return Results.Ok(new { Upvotes = result.Value });
    }
    return Results.BadRequest(new { Error = result.Error });
})
.WithName("ToggleUpvote")
.WithOpenApi();

app.MapGet("/api/products/my-votes", async (HttpContext context, JwtTokenValidator jwt, ProductDbContext db) =>
{
    var userId = jwt.GetUserId(context);
    if (userId == null) return Results.Unauthorized();

    var votedProductIds = await db.ProductUpvotes
        .Where(u => u.UserId == userId)
        .Select(u => u.ProductItemId)
        .ToListAsync();

    return Results.Ok(votedProductIds);
})
.WithName("GetMyVotes")
.WithOpenApi();

app.MapGet("/api/products/upvoted", async (HttpContext context, JwtTokenValidator jwt, ProductDbContext db) =>
{
    var userId = jwt.GetUserId(context);
    if (userId == null) return Results.Unauthorized();

    var votedProductIds = await db.ProductUpvotes
        .Where(u => u.UserId == userId)
        .Select(u => u.ProductItemId)
        .ToListAsync();

    var products = await db.Products
        .Include(p => p.Topics)
        .Include(p => p.Upvotes)
        .Where(p => votedProductIds.Contains(p.Id) && p.Status == Vitrin.Product.Domain.Entities.ProductStatus.Published)
        .Select(p => new {
            p.Id, p.Name, p.Slug, p.Tagline, p.Description, p.ThumbnailUrl, p.GalleryUrls, p.MakerId, p.Status, p.CreatedAt, p.PublishedAt, p.Topics,
            Upvotes = p.Upvotes.Count
        })
        .ToListAsync();
        
    return Results.Ok(products);
})
.WithName("GetUpvotedProducts")
.WithOpenApi();

app.MapGet("/api/products/maker/{makerId}", async (Guid makerId, ProductDbContext db) =>
{
    var products = await db.Products
        .Include(p => p.Topics)
        .Include(p => p.Upvotes)
        .Where(p => p.MakerId == makerId)
        .Select(p => new {
            p.Id, p.Name, p.Slug, p.Tagline, p.Description, p.ThumbnailUrl, p.GalleryUrls, p.MakerId, p.Status, p.CreatedAt, p.PublishedAt, p.Topics,
            Upvotes = p.Upvotes.Count
        })
        .ToListAsync();
        
    return Results.Ok(products);
})
.WithName("GetMakerProducts")
.WithOpenApi();

app.MapGet("/api/products/admin/pending", async (HttpContext context, JwtTokenValidator jwt, ProductDbContext db) =>
{
    var role = jwt.GetRole(context);
    if (role != "Admin") return Results.Unauthorized();

    // ProductStatus.UnderReview is 1
    var products = await db.Products
        .Where(p => p.Status == Vitrin.Product.Domain.Entities.ProductStatus.UnderReview)
        .Select(p => new {
            p.Id, p.Name, p.Slug, p.Tagline, p.Description, p.ThumbnailUrl, p.GalleryUrls, p.MakerId, p.Status, p.CreatedAt, p.PublishedAt
        })
        .ToListAsync();
    return Results.Ok(products);
})
.WithName("GetPendingProducts")
.WithOpenApi();

app.MapPost("/api/products/admin/{id}/approve", async (Guid id, HttpContext context, JwtTokenValidator jwt, ProductDbContext db, Vitrin.Product.Infrastructure.Kafka.ProductEventPublisher eventPublisher) =>
{
    var role = jwt.GetRole(context);
    if (role != "Admin") return Results.Unauthorized();

    var product = await db.Products.FindAsync(id);
    if (product == null) return Results.NotFound();
    
    var result = product.Approve();
    if (result.IsFailure) return Results.BadRequest(new { Error = result.Error });
    
    await db.SaveChangesAsync();

    // Kafka'ya ProductPublishedEvent publish et — Notification consumer karşılar
    await eventPublisher.PublishProductPublished(new Vitrin.Shared.Contracts.Events.ProductPublishedEvent
    {
        ProductId   = product.Id,
        MakerId     = product.MakerId,
        ProductName = product.Name,
        ProductSlug = product.Slug
    });

    return Results.Ok(new { Message = "Product approved successfully!" });
})
.WithName("ApproveProduct")
.WithOpenApi();

app.MapPost("/api/products/admin/{id}/reject", async (Guid id, HttpContext context, JwtTokenValidator jwt, ProductDbContext db) =>
{
    var role = jwt.GetRole(context);
    if (role != "Admin") return Results.Unauthorized();

    var product = await db.Products.FindAsync(id);
    if (product == null) return Results.NotFound();
    
    var result = product.Reject();
    if (result.IsFailure) return Results.BadRequest(new { Error = result.Error });
    
    await db.SaveChangesAsync();
    return Results.Ok(new { Message = "Product rejected successfully!" });
})
.WithName("RejectProduct")
.WithOpenApi();

app.MapGet("/api/products/search", async (string q, ProductDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(q)) return Results.Ok(new List<object>());
    
    var lowerQ = q.ToLower();

    var query = db.Products
        .Include(p => p.Topics)
        .Include(p => p.Upvotes)
        .Where(p => p.Status == Vitrin.Product.Domain.Entities.ProductStatus.Published)
        .Where(p => p.Name.ToLower().Contains(lowerQ) || 
                    p.Tagline.ToLower().Contains(lowerQ) || 
                    p.Description.ToLower().Contains(lowerQ) || 
                    p.Topics.Any(t => t.Name.ToLower().Contains(lowerQ)));

    var products = await query
        .Select(p => new {
            p.Id, p.Name, p.Slug, p.Tagline, p.Description, p.ThumbnailUrl, p.GalleryUrls, p.MakerId, p.Status, p.CreatedAt, p.PublishedAt, p.Topics,
            Upvotes = p.Upvotes.Count
        })
        .ToListAsync();
        
    return Results.Ok(products);
})
.WithName("SearchProducts")
.WithOpenApi();

// --- COLLECTIONS ---

app.MapGet("/api/collections", async (ProductDbContext db) =>
{
    var collections = await db.Collections
        .Include(c => c.Products)
        .OrderByDescending(c => c.CreatedAt)
        .Select(c => new {
            c.Id, c.Name, c.Slug, c.Description, c.UserId, c.CreatedAt, ProductCount = c.Products.Count
        })
        .ToListAsync();
    return Results.Ok(collections);
});

app.MapGet("/api/collections/user/{userId}", async (Guid userId, ProductDbContext db) =>
{
    var collections = await db.Collections
        .Include(c => c.Products)
        .Where(c => c.UserId == userId)
        .OrderByDescending(c => c.CreatedAt)
        .Select(c => new {
            c.Id, c.Name, c.Slug, c.Description, c.UserId, c.CreatedAt, ProductCount = c.Products.Count
        })
        .ToListAsync();
    return Results.Ok(collections);
});

app.MapGet("/api/collections/by-slug/{slug}", async (string slug, ProductDbContext db) =>
{
    var collection = await db.Collections
        .Include(c => c.Products)
        .ThenInclude(p => p.Topics)
        .Include(c => c.Products)
        .ThenInclude(p => p.Upvotes)
        .FirstOrDefaultAsync(c => c.Slug == slug);
        
    if (collection == null) return Results.NotFound();
    
    return Results.Ok(new {
        collection.Id, collection.Name, collection.Slug, collection.Description, collection.UserId, collection.CreatedAt,
        Products = collection.Products.Select(p => new {
            p.Id, p.Name, p.Slug, p.Tagline, p.Description, p.ThumbnailUrl, p.GalleryUrls, p.MakerId, p.Status, p.CreatedAt, p.PublishedAt, p.Topics,
            Upvotes = p.Upvotes.Count
        })
    });
});

app.MapPost("/api/collections", async ([Microsoft.AspNetCore.Mvc.FromBody] CreateCollectionRequest request, ProductDbContext db) =>
{
    var slug = request.Name.ToLower().Replace(" ", "-").Replace("ç", "c").Replace("ğ", "g").Replace("ı", "i").Replace("ö", "o").Replace("ş", "s").Replace("ü", "u");
    
    // Ensure slug is unique
    var baseSlug = slug;
    int counter = 1;
    while (await db.Collections.AnyAsync(c => c.Slug == slug))
    {
        slug = $"{baseSlug}-{counter}";
        counter++;
    }
    
    var collection = Vitrin.Product.Domain.Entities.Collection.Create(request.UserId, request.Name, slug, request.Description);
    db.Collections.Add(collection);
    await db.SaveChangesAsync();
    
    return Results.Ok(collection);
});

app.MapPost("/api/collections/{id}/products/{productId}", async (Guid id, Guid productId, ProductDbContext db) =>
{
    var collection = await db.Collections.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
    if (collection == null) return Results.NotFound("Collection not found");
    
    var product = await db.Products.FindAsync(productId);
    if (product == null) return Results.NotFound("Product not found");
    
    collection.AddProduct(product);
    await db.SaveChangesAsync();
    
    return Results.Ok(new { Message = "Product added to collection" });
});

app.MapDelete("/api/collections/{id}/products/{productId}", async (Guid id, Guid productId, ProductDbContext db) =>
{
    var collection = await db.Collections.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
    if (collection == null) return Results.NotFound("Collection not found");
    
    collection.RemoveProduct(productId);
    await db.SaveChangesAsync();
    
    return Results.Ok(new { Message = "Product removed from collection" });
});

app.Run();

public record CreateCollectionRequest(Guid UserId, string Name, string Description);
