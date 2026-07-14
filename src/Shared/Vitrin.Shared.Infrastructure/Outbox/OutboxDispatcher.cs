using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitrin.Shared.Infrastructure.Kafka;

namespace Vitrin.Shared.Infrastructure.Outbox;

public sealed class OutboxDispatcher<TDbContext>(
    IServiceScopeFactory scopeFactory,
    IEventPublisher eventPublisher,
    IOptions<OutboxOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxDispatcher<TDbContext>> logger) : BackgroundService
    where TDbContext : DbContext
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox polling failed and will be retried.");
            }

            await Task.Delay(options.Value.PollingIntervalMs, stoppingToken);
        }
    }

    internal async Task DispatchPendingBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var messages = await dbContext.Set<OutboxMessage>()
            .Where(message =>
                message.ProcessedAtUtc == null &&
                message.DeadLetteredAtUtc == null &&
                message.NextAttemptAtUtc <= now)
            .OrderBy(message => message.CreatedAtUtc)
            .Take(options.Value.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await eventPublisher.PublishRawAsync(
                    new SerializedIntegrationEvent(
                        message.Id,
                        message.EventType,
                        message.EventVersion,
                        message.OccurredAtUtc,
                        message.CorrelationId,
                        message.CausationId,
                        message.Topic,
                        message.Payload),
                    cancellationToken);

                message.MarkProcessed(timeProvider.GetUtcNow().UtcDateTime);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                message.MarkFailed(
                    ex.Message,
                    timeProvider.GetUtcNow().UtcDateTime,
                    options.Value.MaxRetryAttempts,
                    TimeSpan.FromSeconds(options.Value.MaxBackoffSeconds));

                logger.LogError(
                    ex,
                    "Outbox dispatch failed. EventId={EventId}, EventType={EventType}, RetryCount={RetryCount}",
                    message.Id,
                    message.EventType,
                    message.RetryCount);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

public static class OutboxDependencyInjection
{
    public static IServiceCollection AddVitrinOutbox<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TDbContext : DbContext
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<OutboxOptions>()
            .Bind(configuration.GetSection(OutboxOptions.SectionName))
            .Validate(options => options.PollingIntervalMs > 0)
            .Validate(options => options.BatchSize > 0)
            .Validate(options => options.MaxRetryAttempts > 0)
            .Validate(options => options.MaxBackoffSeconds > 0)
            .ValidateOnStart();
        services.AddHostedService<OutboxDispatcher<TDbContext>>();
        return services;
    }
}
