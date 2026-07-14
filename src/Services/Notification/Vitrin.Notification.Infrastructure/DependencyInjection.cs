using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitrin.Notification.Application.Commands;
using Vitrin.Notification.Infrastructure.Data;
using Vitrin.Notification.Infrastructure.Kafka;
using Vitrin.Notification.Infrastructure.Repositories;
using Vitrin.Shared.Infrastructure.Kafka;

namespace Vitrin.Notification.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core / SQLite
        services.AddDbContext<NotificationDbContext>(options =>
            options.UseSqlite(
                configuration.GetConnectionString("DefaultConnection")
                ?? "Data Source=notification_db.sqlite"));

        // Repository
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.TryAddSingleton(TimeProvider.System);

        // Kafka Consumer — BackgroundService
        services.AddHostedService<NotificationKafkaConsumer>();

        return services;
    }
}
