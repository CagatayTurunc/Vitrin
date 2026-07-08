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

// EF Core SQLite (Docker ve Postgres kurmaya gerek kalmadan test için)
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseSqlite("Data Source=product_db.sqlite"));

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
    var products = await db.Products.ToListAsync();
    return Results.Ok(products);
})
.WithName("GetProducts")
.WithOpenApi();

app.Run();
