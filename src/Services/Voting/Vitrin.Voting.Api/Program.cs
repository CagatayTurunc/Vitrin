using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vitrin.Voting.Application.Commands;
using Vitrin.Voting.Infrastructure;
using Vitrin.Voting.Infrastructure.Data;
using Vitrin.Shared.Infrastructure.Auth;
using Vitrin.Shared.Infrastructure.Api;
using Vitrin.Shared.Infrastructure.Migrations;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Vitrin Voting API", Version = "v1" }); c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme { Name = "Authorization", Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = Microsoft.OpenApi.Models.ParameterLocation.Header }); c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement { { new Microsoft.OpenApi.Models.OpenApiSecurityScheme { Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() } }); });
builder.Services.AddHealthChecks();

if (builder.Environment.IsDevelopment())
{
    var dataProtectionPath = Path.Combine(builder.Environment.ContentRootPath, ".data-protection");
    Directory.CreateDirectory(dataProtectionPath);

    builder.Services
        .AddDataProtection()
        .SetApplicationName("Vitrin.Voting")
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));
}

builder.Services.AddVitrinJwtAuthentication(builder.Configuration);
builder.Services.AddVitrinApiErrors();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(AddVoteCommand).Assembly));

// Infrastructure: DbContext + Repository + Kafka Publisher
builder.Services.AddVotingInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseVitrinApiErrors();

if (await app.MigrateDatabaseAndExitAsync<VoteDbContext>(
    args,
    static (db, cancellationToken) => db.Database.MigrateAsync(cancellationToken))) return;

if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

// Oy ekle
app.MapPost("/api/votes", async (HttpContext context, [FromBody] VoteRequest request, IMediator mediator) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var command = new AddVoteCommand(userId.Value, request.ProductId);
    var result = await mediator.Send(command);
    return result.IsSuccess
        ? Results.Ok(new { Message = "Vote added successfully!" })
        : ApiProblemResults.BadRequest(result.Error, "vote.add_failed");
})
.WithName("AddVote")
.WithOpenApi()
.RequireAuthorization();

// Oy geri al
app.MapDelete("/api/votes", async (HttpContext context, [FromBody] VoteRequest request, IMediator mediator) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var command = new RemoveVoteCommand(userId.Value, request.ProductId);
    var result = await mediator.Send(command);
    return result.IsSuccess
        ? Results.Ok(new { Message = "Vote removed successfully!" })
        : ApiProblemResults.BadRequest(result.Error, "vote.remove_failed");
})
.WithName("RemoveVote")
.WithOpenApi()
.RequireAuthorization();

app.MapGet("/api/votes/me", async (HttpContext context, VoteDbContext db) =>
{
    var userId = context.User.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var productIds = await db.Votes
        .AsNoTracking()
        .Where(vote => vote.UserId == userId.Value)
        .Select(vote => vote.ProductId)
        .ToListAsync(context.RequestAborted);

    return Results.Ok(productIds);
})
.WithName("GetMyVotes")
.WithOpenApi()
.RequireAuthorization();

// Tüm oyları listele (debug / admin)
app.MapGet("/api/votes", async (VoteDbContext db) =>
{
    var votes = await db.Votes
        .AsNoTracking()
        .OrderByDescending(vote => vote.CreatedAt)
        .Take(500)
        .ToListAsync();
    return Results.Ok(votes);
})
.WithName("GetVotes")
.WithOpenApi()
.RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

// Belirli ürünün oy sayısı
app.MapGet("/api/votes/count/{productId:guid}", async (Guid productId, VoteDbContext db) =>
{
    var count = await db.Votes
        .AsNoTracking()
        .CountAsync(v => v.ProductId == productId);
    return Results.Ok(new { ProductId = productId, Count = count });
})
.WithName("GetVoteCount")
.WithOpenApi();

app.MapGet("/api/votes/admin/fraud-signals", async (int? hours, VoteDbContext db, HttpContext context) =>
{
    var requestedHours = Math.Clamp(hours ?? 24, 1, 168);
    var now = DateTime.UtcNow;
    var windowStart = now.AddHours(-requestedHours);
    var burstStart = now.AddMinutes(-15);

    var rapidVoters = await db.Votes
        .AsNoTracking()
        .Where(vote => vote.CreatedAt >= windowStart)
        .GroupBy(vote => vote.UserId)
        .Select(group => new
        {
            UserId = group.Key,
            VoteCount = group.Count(),
            ProductCount = group.Select(vote => vote.ProductId).Distinct().Count(),
            FirstVoteAtUtc = group.Min(vote => vote.CreatedAt),
            LastVoteAtUtc = group.Max(vote => vote.CreatedAt)
        })
        .Where(signal => signal.VoteCount >= 20)
        .OrderByDescending(signal => signal.VoteCount)
        .Take(100)
        .ToListAsync(context.RequestAborted);

    var productBursts = await db.Votes
        .AsNoTracking()
        .Where(vote => vote.CreatedAt >= burstStart)
        .GroupBy(vote => vote.ProductId)
        .Select(group => new
        {
            ProductId = group.Key,
            VoteCount = group.Count(),
            UniqueUserCount = group.Select(vote => vote.UserId).Distinct().Count(),
            FirstVoteAtUtc = group.Min(vote => vote.CreatedAt),
            LastVoteAtUtc = group.Max(vote => vote.CreatedAt)
        })
        .Where(signal => signal.VoteCount >= 10)
        .OrderByDescending(signal => signal.VoteCount)
        .Take(100)
        .ToListAsync(context.RequestAborted);

    return Results.Ok(new
    {
        GeneratedAtUtc = now,
        WindowHours = requestedHours,
        Rules = new
        {
            RapidVoter = "20 veya daha fazla oy / seçilen dönem",
            ProductBurst = "15 dakikada 10 veya daha fazla oy",
            UniqueVoteConstraint = true
        },
        Summary = new
        {
            RapidVoterCount = rapidVoters.Count,
            ProductBurstCount = productBursts.Count,
            TotalSignals = rapidVoters.Count + productBursts.Count
        },
        RapidVoters = rapidVoters,
        ProductBursts = productBursts
    });
})
.WithName("GetVoteFraudSignals")
.WithOpenApi()
.RequireAuthorization(VitrinAuthDefaults.AdminPolicy);

app.Run();

public record VoteRequest(Guid ProductId);
