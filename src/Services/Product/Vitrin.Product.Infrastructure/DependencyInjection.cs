using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitrin.Product.Application.Commands;
using Vitrin.Product.Infrastructure.Data;
using Vitrin.Product.Infrastructure.Kafka;
using Vitrin.Product.Infrastructure.Repositories;
using Vitrin.Shared.Infrastructure.Kafka;
using Vitrin.Shared.Infrastructure.Outbox;

namespace Vitrin.Product.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddProductInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Product veritabanı bağlantı bilgisi yapılandırılmalıdır.");
        }

        // EF Core / PostgreSQL
        services.AddDbContext<ProductDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Repository
        services.AddScoped<IProductRepository, ProductRepository>();

        // Kafka Producer (Shared)
        services.AddSingleton<IEventPublisher, KafkaProducer>();

        services.AddScoped<ProductEventPublisher>();
        services.AddVitrinOutbox<ProductDbContext>(configuration);
        services.AddHostedService<ScheduledLaunchWorker>();

        // Kafka Consumer — Voting servisinden gelen VoteAdded/VoteRemoved event'lerini dinler
        services.AddHostedService<VotingEventsConsumer>();
        services.AddHostedService<EngagementEventsConsumer>();

        return services;
    }
}
