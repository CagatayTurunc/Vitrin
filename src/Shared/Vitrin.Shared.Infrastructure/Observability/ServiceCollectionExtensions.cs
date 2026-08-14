using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Elasticsearch;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using System.Diagnostics;
using System.Reflection;

namespace Vitrin.Shared.Infrastructure.Observability;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Vitrin observability stack'ini ekler: Serilog, OpenTelemetry, Metrics
    /// </summary>
    public static IServiceCollection AddVitrinObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        string serviceName)
    {
        // Activity Source for OpenTelemetry
        var activitySource = new ActivitySource($"Vitrin.{serviceName}");
        services.AddSingleton(activitySource);

        // Serilog Configuration
        services.AddSerilog((serviceProvider, loggerConfiguration) =>
        {
            var config = serviceProvider.GetRequiredService<IConfiguration>();
            
            loggerConfiguration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", serviceName)
                .Enrich.WithProperty("Environment", environment.EnvironmentName)
                .Enrich.WithProperty("Version", Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown");

            // Console logging for development
            if (environment.IsDevelopment())
            {
                loggerConfiguration.WriteTo.Console();
            }

            // File logging
            loggerConfiguration.WriteTo.File(
                path: $"logs/vitrin-{serviceName}-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                fileSizeLimitBytes: 100_000_000, // 100MB
                rollOnFileSizeLimit: true);

            // Elasticsearch logging for production
            var elasticsearchUrl = config.GetConnectionString("Elasticsearch");
            if (!string.IsNullOrEmpty(elasticsearchUrl))
            {
                loggerConfiguration.WriteTo.Elasticsearch(new Serilog.Sinks.Elasticsearch.ElasticsearchSinkOptions(new Uri(elasticsearchUrl))
                {
                    IndexFormat = $"vitrin-logs-{serviceName.ToLower()}-{DateTime.UtcNow:yyyy-MM}",
                    AutoRegisterTemplate = true,
                    AutoRegisterTemplateVersion = Serilog.Sinks.Elasticsearch.AutoRegisterTemplateVersion.ESv7,
                    CustomFormatter = new ElasticsearchJsonFormatter(),
                    EmitEventFailure = Serilog.Sinks.Elasticsearch.EmitEventFailureHandling.WriteToSelfLog |
                                     Serilog.Sinks.Elasticsearch.EmitEventFailureHandling.WriteToFailureSink,
                    FailureSink = new Serilog.Sinks.File.FileSink($"logs/elasticsearch-failures-{serviceName}-.log", 
                        new Serilog.Formatting.Json.JsonFormatter(), null)
                });
            }
        });

        // OpenTelemetry Tracing
        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .AddSource($"Vitrin.{serviceName}")
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService(serviceName: $"Vitrin.{serviceName}", serviceVersion: "1.0.0")
                        .AddAttributes(new Dictionary<string, object>
                        {
                            ["deployment.environment"] = environment.EnvironmentName,
                            ["service.instance.id"] = Environment.MachineName
                        }))
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = (httpContext) =>
                        {
                            // Health check endpoint'lerini trace etme
                            return !httpContext.Request.Path.StartsWithSegments("/health") &&
                                   !httpContext.Request.Path.StartsWithSegments("/metrics");
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                        options.SetDbStatementForStoredProcedure = true;
                    });

                // Redis instrumentation
                var redisConnectionString = configuration.GetConnectionString("Redis");
                if (!string.IsNullOrEmpty(redisConnectionString))
                {
                    builder.AddRedisInstrumentation();
                }

                // Jaeger exporter for development
                if (environment.IsDevelopment())
                {
                    builder.AddJaegerExporter();
                }

                // OTLP exporter for production (Jaeger, Grafana Tempo, etc.)
                var otlpEndpoint = configuration.GetValue<string>("OpenTelemetry:OtlpEndpoint");
                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    builder.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });
                }

                // Console exporter for debugging
                if (environment.IsDevelopment())
                {
                    builder.AddConsoleExporter();
                }
            })
            .WithMetrics(builder =>
            {
                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService(serviceName: $"Vitrin.{serviceName}", serviceVersion: "1.0.0"))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation();

                // Prometheus metrics endpoint
                builder.AddPrometheusExporter();

                // Console metrics for development
                if (environment.IsDevelopment())
                {
                    builder.AddConsoleExporter();
                }
            });

        return services;
    }

    /// <summary>
    /// Health check endpoint'lerini ekler
    /// </summary>
    public static IServiceCollection AddVitrinHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        // Database health check
        var defaultConnection = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(defaultConnection))
        {
            if (defaultConnection.Contains("Host=") || defaultConnection.Contains("Server="))
            {
                // PostgreSQL
                healthChecksBuilder.AddNpgSql(defaultConnection, name: "database");
            }
            else if (defaultConnection.Contains("Data Source="))
            {
                // SQLite
                healthChecksBuilder.AddSqlite(defaultConnection, name: "database");
            }
        }

        // Redis health check
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            healthChecksBuilder.AddRedis(redisConnection, name: "redis");
        }

        // Kafka health check
        var kafkaConnection = configuration.GetConnectionString("Kafka");
        if (!string.IsNullOrEmpty(kafkaConnection))
        {
            healthChecksBuilder.AddKafka(options =>
            {
                options.BootstrapServers = kafkaConnection;
            }, name: "kafka");
        }

        return services;
    }
}