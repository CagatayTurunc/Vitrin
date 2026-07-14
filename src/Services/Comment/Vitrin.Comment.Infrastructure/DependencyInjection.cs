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
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Comment veritabanı bağlantı bilgisi yapılandırılmalıdır.");
        }

        // EF Core / PostgreSQL
        services.AddDbContext<CommentDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Repository
        services.AddScoped<ICommentRepository, CommentRepository>();

        // Kafka Producer (Shared)
        services.AddSingleton<IEventPublisher, KafkaProducer>();

        // Notification publisher — HTTP yerine Kafka
        services.AddScoped<ICommentNotificationPublisher, CommentNotificationPublisher>();

        return services;
    }
}
