using Microsoft.EntityFrameworkCore;
using Vitrin.Ai.Application.Commands;
using Vitrin.Ai.Application.Services;
using Vitrin.Ai.Infrastructure.Data;
using Vitrin.Ai.Infrastructure.Repositories;
using Vitrin.Ai.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AnalyzeProductCommand).Assembly));

builder.Services.AddDbContext<AiDbContext>(options =>
    options.UseSqlite("Data Source=ai_db.sqlite"));

builder.Services.AddScoped<IAiAnalysisRepository, AiRepository>();
builder.Services.AddScoped<IAiAnalyzerService, MockAiAnalyzerService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AiDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/api/ai/analyze", async ([FromBody] AnalyzeProductCommand command, IMediator mediator) =>
{
    var result = await mediator.Send(command);
    if (result.IsSuccess)
    {
        return Results.Ok(new { AnalysisId = result.Value, Message = "Product analyzed successfully!" });
    }
    return Results.BadRequest(new { Error = result.Error });
})
.WithName("AnalyzeProduct")
.WithOpenApi();

app.MapGet("/api/ai/product/{productId}", async (Guid productId, AiDbContext db) =>
{
    var analysis = await db.AiAnalysisResults.FirstOrDefaultAsync(a => a.ProductId == productId);
    if (analysis == null)
        return Results.NotFound();
        
    return Results.Ok(new { analysis.Summary, analysis.Tags });
})
.WithName("GetAnalysisResult")
.WithOpenApi();

app.Run();
