using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitrin.Auth.Application.Interfaces;
using Vitrin.Auth.Infrastructure.Data;
using Vitrin.Auth.Infrastructure.Audit;
using Vitrin.Auth.Infrastructure.Kafka;
using Vitrin.Auth.Infrastructure.Repositories;
using Vitrin.Auth.Infrastructure.Services;
using Vitrin.Shared.Infrastructure.Kafka;
using Vitrin.Shared.Infrastructure.Audit;
using Vitrin.Shared.Infrastructure.Outbox;

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

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IJwtProvider, JwtProvider>();
        services.AddSingleton<IExternalIdentityVerifier>(_ =>
            new ExternalIdentityVerifier(
                configuration,
                new HttpClient { Timeout = TimeSpan.FromSeconds(10) }));

        // Kafka Producer + Notification Publisher
        services.AddSingleton<IEventPublisher, KafkaProducer>();
        services.AddScoped<IAuthNotificationPublisher, AuthNotificationPublisher>();
        services.AddScoped<IAuditLogger, AuthAuditLogger>();
        services.AddVitrinOutbox<AuthDbContext>(configuration);

        return services;
    }
}
