using Microsoft.EntityFrameworkCore;
using Vitrin.Product.Application.Commands;
using Vitrin.Product.Infrastructure.Data;
using Vitrin.Product.Infrastructure.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateProductCommand).Assembly));

// EF Core PostgreSQL
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Database=vitrin_product;Username=postgres;Password=123456"));

// Register Real Repository
builder.Services.AddScoped<IProductRepository, ProductRepository>();

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

app.MapGet("/api/products", async (ProductDbContext db) =>
{
    // ProductStatus.Published is 2
    var products = await db.Products
        .Include(p => p.Topics)
        .Include(p => p.Upvotes)
        .Where(p => p.Status == Vitrin.Product.Domain.Entities.ProductStatus.Published)
        .Select(p => new {
            p.Id, p.Name, p.Slug, p.Tagline, p.Description, p.ThumbnailUrl, p.GalleryUrls, p.MakerId, p.Status, p.CreatedAt, p.PublishedAt, p.Topics,
            Upvotes = p.Upvotes.Count
        })
        .ToListAsync();
    return Results.Ok(products);
})
.WithName("GetProducts")
.WithOpenApi();

Guid? GetUserIdFromRequest(HttpContext context)
{
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
    if (authHeader != null && authHeader.StartsWith("Bearer "))
    {
        var token = authHeader.Substring("Bearer ".Length);
        try {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(sub, out var userId)) return userId;
        } catch { }
    }
    return null;
}

app.MapPost("/api/products/{id}/vote", async (Guid id, HttpContext context, IMediator mediator) =>
{
    var userId = GetUserIdFromRequest(context);
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

app.MapGet("/api/products/my-votes", async (HttpContext context, ProductDbContext db) =>
{
    var userId = GetUserIdFromRequest(context);
    if (userId == null) return Results.Unauthorized();

    var votedProductIds = await db.ProductUpvotes
        .Where(u => u.UserId == userId)
        .Select(u => u.ProductItemId)
        .ToListAsync();

    return Results.Ok(votedProductIds);
})
.WithName("GetMyVotes")
.WithOpenApi();

app.MapGet("/api/products/admin/pending", async (ProductDbContext db) =>
{
    // ProductStatus.UnderReview is 1
    var products = await db.Products.Where(p => p.Status == Vitrin.Product.Domain.Entities.ProductStatus.UnderReview).ToListAsync();
    return Results.Ok(products);
})
.WithName("GetPendingProducts")
.WithOpenApi();

app.MapPost("/api/products/admin/{id}/approve", async (Guid id, ProductDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product == null) return Results.NotFound();
    
    var result = product.Approve();
    if (result.IsFailure) return Results.BadRequest(new { Error = result.Error });
    
    await db.SaveChangesAsync();
    return Results.Ok(new { Message = "Product approved successfully!" });
})
.WithName("ApproveProduct")
.WithOpenApi();

app.MapPost("/api/products/admin/{id}/reject", async (Guid id, ProductDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product == null) return Results.NotFound();
    
    var result = product.Reject();
    if (result.IsFailure) return Results.BadRequest(new { Error = result.Error });
    
    await db.SaveChangesAsync();
    return Results.Ok(new { Message = "Product rejected successfully!" });
})
.WithName("RejectProduct")
.WithOpenApi();

app.Run();
