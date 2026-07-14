using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitrin.Auth.Application.Interfaces;
using Vitrin.Auth.Infrastructure.Data;
using Vitrin.Auth.Infrastructure.Kafka;
using Vitrin.Auth.Infrastructure.Repositories;
using Vitrin.Auth.Infrastructure.Services;
using Vitrin.Shared.Infrastructure.Kafka;

namespace Vitrin.Auth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Database=vitrin_auth;Username=postgres;Password=123456"));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IJwtProvider, JwtProvider>();

        // Kafka Producer + Notification Publisher
        services.AddSingleton<IEventPublisher, KafkaProducer>();
        services.AddScoped<IAuthNotificationPublisher, AuthNotificationPublisher>();

        return services;
    }
}
