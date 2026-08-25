using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Vitrin.Shared.Infrastructure.Api;

/// <summary>
/// Tüm Vitrin servislerinde ortak Swagger / OpenAPI konfigürasyonu.
///
/// Her servis Program.cs'te şu şekilde kullanır:
///   builder.Services.AddVitrinSwagger("Vitrin Auth API");
///   ...
///   app.UseVitrinSwagger(app.Environment, "v1", "/api/auth");
///
/// Eklenen özellikler:
/// - JWT Bearer security scheme ("Authorize" butonu ile token girişi)
/// - API bilgileri (başlık, sürüm, açıklama, iletişim)
/// - RFC 7807 ProblemDetails response örnekleri
/// - v1 versiyonu otomatik seçili
/// </summary>
public static class VitrinSwaggerExtensions
{
    private const string SecuritySchemeName = "Bearer";

    public static IServiceCollection AddVitrinSwagger(
        this IServiceCollection services,
        string title,
        string description = "")
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            // ── API Bilgileri ────────────────────────────────────────────────
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title       = title,
                Version     = "v1",
                Description = string.IsNullOrWhiteSpace(description)
                    ? $"{title} — Vitrin mikroservis platformu."
                    : description,
                Contact = new OpenApiContact
                {
                    Name = "Vitrin",
                    Url  = new Uri("https://vitrin.it.com")
                }
            });

            // ── JWT Bearer Security Scheme ───────────────────────────────────
            // "Authorize" butonu ile tüm korumalı endpoint'ler test edilebilir.
            // Değer formatı: Bearer <token>  (prefix otomatik eklenir)
            c.AddSecurityDefinition(SecuritySchemeName, new OpenApiSecurityScheme
            {
                Name         = "Authorization",
                Type         = SecuritySchemeType.Http,
                Scheme       = "bearer",
                BearerFormat = "JWT",
                In           = ParameterLocation.Header,
                Description  =
                    "JWT token'ı girin. Örnek: `eyJhbGci...`\n\n" +
                    "Token almak için önce `/api/auth/login` endpoint'ini çağırın."
            });

            // Tüm operasyonlara güvenlik gereksinimi ekle —
            // RequireAuthorization() olmayan endpoint'ler için Swagger
            // opsiyonel olarak token göndermeye çalışır (hata üretmez).
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id   = SecuritySchemeName
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // ── Ortak response şemaları ──────────────────────────────────────
            // Tüm servislerde RFC 7807 ProblemDetails döner; Swagger bunu gösterir.
            c.MapType<Microsoft.AspNetCore.Mvc.ProblemDetails>(() => new OpenApiSchema
            {
                Type = "object",
                Description = "RFC 7807 ProblemDetails — standart hata formatı",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["status"]   = new() { Type = "integer", Example = new Microsoft.OpenApi.Any.OpenApiInteger(400) },
                    ["title"]    = new() { Type = "string",  Example = new Microsoft.OpenApi.Any.OpenApiString("The request could not be processed.") },
                    ["detail"]   = new() { Type = "string",  Example = new Microsoft.OpenApi.Any.OpenApiString("Validation failed.") },
                    ["code"]     = new() { Type = "string",  Example = new Microsoft.OpenApi.Any.OpenApiString("request.invalid") },
                    ["traceId"]  = new() { Type = "string",  Example = new Microsoft.OpenApi.Any.OpenApiString("00-abc123def456-01") },
                    ["instance"] = new() { Type = "string",  Example = new Microsoft.OpenApi.Any.OpenApiString("/api/auth/login") }
                }
            });

            // Enum değerlerini sayı yerine isimle göster
            c.UseInlineDefinitionsForEnums();

            // Aynı isimli route'larda çakışmayı önle
            c.CustomSchemaIds(type => type.FullName?.Replace("+", "_") ?? type.Name);
        });

        return services;
    }

    /// <summary>
    /// Swagger UI middleware'ini ekler — sadece Development ortamında aktif.
    /// </summary>
    /// <param name="app">Web uygulaması</param>
    /// <param name="environment">Host environment</param>
    /// <param name="routePrefix">Swagger UI URL prefix'i (varsayılan: "swagger")</param>
    public static IApplicationBuilder UseVitrinSwagger(
        this IApplicationBuilder app,
        IHostEnvironment environment,
        string routePrefix = "swagger")
    {
        if (!environment.IsDevelopment())
            return app;

        app.UseSwagger(c =>
        {
            c.RouteTemplate = $"{routePrefix}/{{documentName}}/openapi.json";
        });

        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint($"/{routePrefix}/v1/openapi.json", "v1");
            c.RoutePrefix           = routePrefix;
            c.DocumentTitle         = "Vitrin API";
            c.DefaultModelsExpandDepth(-1);          // Şema bölümünü gizle (dağınıklık azalır)
            c.DefaultModelRendering(ModelRendering.Example); // Örnek JSON önce göster
            c.DisplayRequestDuration = true;          // Her istek süresini göster
            c.EnableDeepLinking();                    // Direkt endpoint URL'i paylaşılabilir
            c.EnableFilter();                         // Endpoint arama kutusu
            c.DocExpansion(DocExpansion.List);        // Endpoint listesi kapalı başlar
        });

        return app;
    }
}
