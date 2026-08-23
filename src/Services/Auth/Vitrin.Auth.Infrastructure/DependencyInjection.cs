using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Vitrin.Auth.Application.Interfaces;
using Vitrin.Auth.Infrastructure.Data;
using Vitrin.Auth.Infrastructure.Audit;
using Vitrin.Auth.Infrastructure.Kafka;
using Vitrin.Auth.Infrastructure.Repositories;
using Vitrin.Auth.Infrastructure.Services;
using Vitrin.Shared.Infrastructure.Kafka;
using Vitrin.Shared.Infrastructure.Audit;
using Vitrin.Shared.Infrastructure.Auth;
using Vitrin.Shared.Infrastructure.Outbox;
using Microsoft.Extensions.Hosting;

namespace Vitrin.Auth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Auth veritabanı bağlantı bilgisi yapılandırılmalıdır.");
        }

        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Madde 4 — Redis token blacklist (Auth servisi de logout için kullanır)
        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnection));
        services.AddVitrinTokenBlacklist();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IJwtProvider, JwtProvider>();
        services.AddSingleton<IAccountActionTokenService, AccountActionTokenService>();
        services.AddHttpClient<IAccountEmailService, ResendAccountEmailService>(client =>
        {
            client.BaseAddress = new Uri("https://api.resend.com/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddSingleton<IExternalIdentityVerifier>(_ =>
            new ExternalIdentityVerifier(
                configuration,
                new HttpClient { Timeout = TimeSpan.FromSeconds(10) }));

        // Kafka Producer + Notification Publisher
        services.AddSingleton<IEventPublisher, KafkaProducer>();
        services.AddScoped<IAuthNotificationPublisher, AuthNotificationPublisher>();
        services.AddScoped<IAuditLogger, AuthAuditLogger>();
        services.AddVitrinOutbox<AuthDbContext>(configuration);

        // KVKK — her gün 30 günü dolan silme taleplerini anonimleştirir
        services.AddHostedService<RetentionCleanupWorker>();

        return services;
    }
}
