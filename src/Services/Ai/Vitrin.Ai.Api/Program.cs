using Microsoft.EntityFrameworkCore;
using Vitrin.Ai.Application.Commands;
using Vitrin.Ai.Application.Services;
using Vitrin.Ai.Infrastructure.Data;
using Vitrin.Ai.Infrastructure.Repositories;
using Vitrin.Ai.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vitrin.Shared.Infrastructure.Auth;
using Vitrin.Shared.Infrastructure.Api;
using Vitrin.Shared.Infrastructure.Audit;
using Vitrin.Shared.Infrastructure.Migrations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddVitrinJwtAuthentication(builder.Configuration);
builder.Services.AddVitrinApiErrors();
builder.Services.AddVitrinRateLimiting();
builder.Services.AddVitrinAuditLogging();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AnalyzeProductCommand).Assembly));

builder.Services.AddDbContext<AiDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=ai_db.sqlite"));

builder.Services.AddScoped<IAiAnalysisRepository, AiRepository>();
builder.Services.AddScoped<IAiQuotaService, AiQuotaService>();
builder.Services.AddHttpClient<IAiAnalyzerService, GeminiAiAnalyzerService>();
builder.Services.AddOptions<AiQuotaOptions>()
    .Bind(builder.Configuration.GetSection(AiQuotaOptions.SectionName))
    .Validate(options => options.DailyRequestLimit > 0, "AI daily request limit must be greater than zero.")
    .ValidateOnStart();

var app = builder.Build();

app.UseVitrinApiErrors();

if (await app.MigrateDatabaseAndExitAsync<AiDbContext>(
    args,
    static (db, cancellationToken) => db.Database.MigrateAsync(cancellationToken))) return;

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapPost("/api/ai/analyze", async ([FromBody] AnalyzeProductCommand command, HttpContext context, IMediator mediator, IAiQuotaService quotaService, IAuditLogger auditLogger) =>
{
    var validationErrors = ValidateAnalysisRequest(command);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(
            validationErrors,
            title: "One or more validation errors occurred.",
            extensions: ApiProblemResults.Extensions("request.validation_failed"));
    }

    var userId = context.User.GetUserId();
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var quota = await quotaService.TryConsumeAsync(userId.Value, context.RequestAborted);
    if (!quota.IsAllowed)
    {
        await auditLogger.WriteAsync(
            new AuditEvent("ai.analysis_quota_denied", userId, "Product", command.ProductId.ToString(), "Denied", context.TraceIdentifier),
            context.RequestAborted);

        return ApiProblemResults.TooManyRequests(
            "Your daily AI analysis quota has been exhausted.",
            "ai.daily_quota_exceeded",
            quota.ResetAtUtc);
    }

    var result = await mediator.Send(command);
    await auditLogger.WriteAsync(
        new AuditEvent("ai.product_analyzed", userId, "Product", command.ProductId.ToString(), result.IsSuccess ? "Succeeded" : "Failed", context.TraceIdentifier),
        context.RequestAborted);

    if (result.IsSuccess)
    {
        return Results.Ok(new
        {
            AnalysisId = result.Value,
            Message = "Product analyzed successfully!",
            QuotaRemaining = quota.RemainingRequests,
            QuotaResetAtUtc = quota.ResetAtUtc
        });
    }
    return ApiProblemResults.BadRequest(result.Error, "ai.analysis_failed");
})
.WithName("AnalyzeProduct")
.WithOpenApi()
.RequireRateLimiting(VitrinRateLimitPolicies.AiAnalysis)
.RequireAuthorization(VitrinAuthDefaults.MakerOrAdminPolicy);

app.MapGet("/api/ai/product/{productId}", async (Guid productId, AiDbContext db) =>
{
    var analysis = await db.AiAnalysisResults
        .AsNoTracking()
        .OrderByDescending(item => item.AnalyzedAt)
        .FirstOrDefaultAsync(a => a.ProductId == productId);
    if (analysis == null)
        return Results.NotFound();
        
    return Results.Ok(new { analysis.Summary, analysis.Tags });
})
.WithName("GetAnalysisResult")
.WithOpenApi();

app.MapGet("/api/ai/product/{productId}/recommendations", async (Guid productId, AiDbContext db) =>
{
    var analysis = await db.AiAnalysisResults
        .AsNoTracking()
        .OrderByDescending(item => item.AnalyzedAt)
        .FirstOrDefaultAsync(a => a.ProductId == productId);
    if (analysis == null || string.IsNullOrEmpty(analysis.Tags))
        return Results.Ok(new List<Guid>());
        
    var tags = analysis.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    
    var allAnalyses = await db.AiAnalysisResults
        .AsNoTracking()
        .Where(a => a.ProductId != productId && !string.IsNullOrEmpty(a.Tags))
        .ToListAsync();
        
    var recommendedIds = allAnalyses
        .Select(a => new {
            a.ProductId,
            SharedTagCount = a.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                   .Count(t => tags.Contains(t, StringComparer.OrdinalIgnoreCase))
        })
        .Where(x => x.SharedTagCount > 0)
        .OrderByDescending(x => x.SharedTagCount)
        .Take(3)
        .Select(x => x.ProductId)
        .ToList();
        
    return Results.Ok(recommendedIds);
})
.WithName("GetRecommendations")
.WithOpenApi();

app.Run();

static Dictionary<string, string[]> ValidateAnalysisRequest(AnalyzeProductCommand command)
{
    var errors = new Dictionary<string, string[]>();

    if (command.ProductId == Guid.Empty)
        errors[nameof(command.ProductId)] = ["A product id is required."];

    if (string.IsNullOrWhiteSpace(command.ProductName) || command.ProductName.Length > 200)
        errors[nameof(command.ProductName)] = ["Product name must contain between 1 and 200 characters."];

    if (string.IsNullOrWhiteSpace(command.ProductDescription) || command.ProductDescription.Length > 10_000)
        errors[nameof(command.ProductDescription)] = ["Product description must contain between 1 and 10000 characters."];

    return errors;
}
