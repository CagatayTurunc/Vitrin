using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitrin.Shared.Infrastructure.Kafka;
using Vitrin.Shared.Infrastructure.Outbox;
using Vitrin.Voting.Application.Commands;
using Vitrin.Voting.Infrastructure.Data;
using Vitrin.Voting.Infrastructure.Kafka;
using Vitrin.Voting.Infrastructure.Repositories;

namespace Vitrin.Voting.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddVotingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core / SQLite
        services.AddDbContext<VoteDbContext>(options =>
            options.UseSqlite(
                configuration.GetConnectionString("DefaultConnection")
                ?? "Data Source=voting_db.sqlite"));

        // Repository
        services.AddScoped<IVoteRepository, VoteRepository>();

        // Kafka Producer (Shared Infrastructure)
        services.AddSingleton<IEventPublisher, KafkaProducer>();

        // Vote event publisher (wraps KafkaProducer)
        services.AddScoped<IVoteEventPublisher, VoteEventPublisher>();

        services.AddVitrinOutbox<VoteDbContext>(configuration);

        return services;
    }
}
