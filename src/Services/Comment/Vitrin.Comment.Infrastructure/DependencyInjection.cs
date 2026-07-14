using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitrin.Comment.Application.Commands;
using Vitrin.Comment.Infrastructure.Data;
using Vitrin.Comment.Infrastructure.Kafka;
using Vitrin.Comment.Infrastructure.Repositories;
using Vitrin.Shared.Infrastructure.Kafka;

namespace Vitrin.Comment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCommentInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core / PostgreSQL
        services.AddDbContext<CommentDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Database=vitrin_comment;Username=postgres;Password=123456"));

        // Repository
        services.AddScoped<ICommentRepository, CommentRepository>();

        // Kafka Producer (Shared)
        services.AddSingleton<IEventPublisher, KafkaProducer>();

        // Notification publisher — HTTP yerine Kafka
        services.AddScoped<ICommentNotificationPublisher, CommentNotificationPublisher>();

        return services;
    }
}
