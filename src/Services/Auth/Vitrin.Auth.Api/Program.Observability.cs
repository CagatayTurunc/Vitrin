// Bu dosya, mevcut Program.cs dosyasının başına eklenecek observability entegrasyonunu gösterir
// Implementasyon için mevcut Program.cs dosyasının başında aşağıdaki değişiklikleri yapın:

using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitrin.Auth.Application;
using Vitrin.Auth.Application.Commands;
using Vitrin.Auth.Infrastructure;
using Vitrin.Auth.Api;
using Vitrin.Shared.Infrastructure.Api;
using Vitrin.Shared.Infrastructure.Audit;
using Vitrin.Shared.Infrastructure.Auth;
using Vitrin.Shared.Infrastructure.Migrations;
// *** OBSERVABILITY IMPORTS - EKLE ***
using Vitrin.Shared.Infrastructure.Observability;
using Serilog;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// *** OBSERVABILITY CONFIGURATION - EKLE ***
// Serilog ve OpenTelemetry entegrasyonunu ekle
builder.Services.AddVitrinObservability(
    builder.Configuration, 
    builder.Environment, 
    "Auth");

// Business metrics collector'ı ekle
builder.Services.AddSingleton<MetricsCollector>(provider => 
    new MetricsCollector("Auth", provider.GetRequiredService<ILogger<MetricsCollector>>()));

// Health checks'i genişlet
builder.Services.AddVitrinHealthChecks(builder.Configuration);

// Mevcut servisler...
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddVitrinApiErrors();
builder.Services.AddVitrinAuditLogging();
builder.Services.AddVitrinJwtAuthentication(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// *** OBSERVABILITY MIDDLEWARE - EKLE ***
// Correlation middleware'i en başta ekle
app.UseVitrinCorrelation();

// Prometheus metrics endpoint'i ekle
app.UseOpenTelemetryPrometheusScrapingEndpoint();

// Mevcut middleware'lar...
app.UseVitrinApiErrors();

// Database migration...
if (await app.MigrateDatabaseAndExitAsync<Vitrin.Auth.Infrastructure.Data.AuthDbContext>(
    args,
    static (db, cancellationToken) => db.Database.MigrateAsync(cancellationToken))) return;

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// Enhanced health checks endpoint
app.MapHealthChecks("/health");

// *** ENHANCED ENDPOINTS WITH OBSERVABILITY - ÖRNEK ***
app.MapPost("/api/auth/register", async (
    RegisterCommand command, 
    HttpContext context, 
    IMediator mediator, 
    IAuditLogger auditLogger,
    MetricsCollector metrics,
    ILogger<Program> logger) =>
{
    using var activity = Activity.Current?.Source.StartActivity("Auth.Register");
    activity?.SetTag("operation", "user_registration");
    
    var stopwatch = Stopwatch.StartNew();
    try
    {
        var result = await mediator.Send(command);
        stopwatch.Stop();
        
        // Business logging
        logger.LogBusinessOperation("UserRegistration", new { command.Email, Source = "direct" });
        
        // Metrics
        metrics.IncrementUserRegistrations("direct");
        metrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "POST", "/api/auth/register", 200);
        
        // Audit logging
        await auditLogger.WriteAsync(
            new AuditEvent("auth.register", null, "Session", null, result.IsSuccess ? "Succeeded" : "Failed", context.TraceIdentifier),
            context.RequestAborted);
            
        if (!result.IsSuccess)
        {
            metrics.IncrementErrors("registration_validation", "register");
            activity?.SetStatus(ActivityStatusCode.Error, result.Error);
        }
        
        return result.IsSuccess ? Results.Ok(result.Value) : ApiProblemResults.BadRequest(result.Error, "auth.registration_failed");
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        metrics.IncrementErrors(ex.GetType().Name, "register");
        metrics.RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, "POST", "/api/auth/register", 500);
        
        logger.LogError(ex, "Registration error", new Dictionary<string, object>
        {
            ["Email"] = command.Email,
            ["Duration"] = stopwatch.ElapsedMilliseconds
        });
        
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        throw;
    }
})
.AddEndpointFilter<ValidationEndpointFilter<RegisterCommand>>();

// *** Diğer endpoint'ler için benzer pattern uygulanabilir ***

app.Run();