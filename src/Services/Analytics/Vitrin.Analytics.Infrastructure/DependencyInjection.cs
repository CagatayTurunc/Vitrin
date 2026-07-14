using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitrin.Analytics.Domain.Repositories;
using Vitrin.Analytics.Infrastructure.Data;
using Vitrin.Analytics.Infrastructure.Kafka;
using Vitrin.Analytics.Infrastructure.Repositories;

namespace Vitrin.Analytics.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAnalyticsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core / SQLite
        services.AddDbContext<AnalyticsDbContext>(options =>
            options.UseSqlite(
                configuration.GetConnectionString("DefaultConnection")
                ?? "Data Source=analytics_db.sqlite"));

        // Repository — domain interface'e bağlı
        services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();

        // Kafka Consumer — BackgroundService olarak kayıt
        services.AddHostedService<AnalyticsKafkaConsumer>();

        return services;
    }
}
