using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitrin.Comment.Application.Commands;
using Vitrin.Comment.Infrastructure.Data;
using Vitrin.Comment.Infrastructure.Kafka;
using Vitrin.Comment.Infrastructure.Repositories;
using Vitrin.Comment.Infrastructure.Services;
using Vitrin.Shared.Infrastructure.Kafka;
using Vitrin.Shared.Infrastructure.Outbox;

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
        services.AddHttpClient<ICommentMentionResolver, AuthCommentMentionResolver>(client =>
        {
            var authBaseUrl = configuration["Services:AuthBaseUrl"] ?? "http://localhost:5104";
            client.BaseAddress = new Uri(authBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(3);
        });
        services.AddVitrinOutbox<CommentDbContext>(configuration);

        return services;
    }
}
