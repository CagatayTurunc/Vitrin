using Microsoft.Extensions.Logging;
using Vitrin.Shared.Contracts.Events;
using Vitrin.Shared.Infrastructure.Kafka;
using Vitrin.Voting.Application.Commands;

namespace Vitrin.Voting.Infrastructure.Kafka;

/// <summary>
/// IVoteEventPublisher'ın Kafka implementasyonu.
/// Voting servisi oy kaydettikten sonra buraya gelir,
/// "voting-events" topic'ine publish eder.
/// </summary>
public class VoteEventPublisher : IVoteEventPublisher
{
    private readonly IEventPublisher _kafkaProducer;
    private readonly ILogger<VoteEventPublisher> _logger;

    public VoteEventPublisher(IEventPublisher kafkaProducer, ILogger<VoteEventPublisher> logger)
    {
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    public async Task PublishVoteAddedAsync(VoteAddedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await _kafkaProducer.PublishAsync(@event);
            _logger.LogInformation(
                "[VoteEventPublisher] VoteAddedEvent published. UserId={UserId}, ProductId={ProductId}",
                @event.UserId, @event.ProductId);
        }
        catch (Exception ex)
        {
            // Kafka geçici olarak düşse bile oy kaydedildi — event kaybı production'da Outbox pattern ile çözülür
            _logger.LogError(ex,
                "[VoteEventPublisher] Failed to publish VoteAddedEvent. UserId={UserId}, ProductId={ProductId}",
                @event.UserId, @event.ProductId);
        }
    }

    public async Task PublishVoteRemovedAsync(VoteRemovedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await _kafkaProducer.PublishAsync(@event);
            _logger.LogInformation(
                "[VoteEventPublisher] VoteRemovedEvent published. UserId={UserId}, ProductId={ProductId}",
                @event.UserId, @event.ProductId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[VoteEventPublisher] Failed to publish VoteRemovedEvent. UserId={UserId}, ProductId={ProductId}",
                @event.UserId, @event.ProductId);
        }
    }
}
