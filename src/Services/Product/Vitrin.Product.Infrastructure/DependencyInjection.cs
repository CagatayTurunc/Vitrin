using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitrin.Product.Application.Commands;
using Vitrin.Product.Infrastructure.Data;
using Vitrin.Product.Infrastructure.Kafka;
using Vitrin.Product.Infrastructure.Repositories;
using Vitrin.Shared.Infrastructure.Kafka;

namespace Vitrin.Product.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddProductInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core / PostgreSQL
        services.AddDbContext<ProductDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Database=vitrin_product;Username=postgres;Password=123456"));

        // Repository
        services.AddScoped<IProductRepository, ProductRepository>();

        // Kafka Producer (Shared)
        services.AddSingleton<IEventPublisher, KafkaProducer>();

        // Product event publisher (analytics + shared events)
        services.AddScoped<IProductEventPublisher, ProductEventPublisher>();

        // Kafka Consumer — Voting servisinden gelen VoteAdded/VoteRemoved event'lerini dinler
        services.AddHostedService<VotingEventsConsumer>();

        return services;
    }
}
