using MediatR;
using Vitrin.Shared.Contracts.Events;
using Vitrin.Shared.Kernel.Results;
using Vitrin.Voting.Domain.Entities;

namespace Vitrin.Voting.Application.Commands;

public interface IVoteRepository
{
    Task<bool> HasUserVotedAsync(Guid userId, Guid productId, CancellationToken cancellationToken);
    Task AddAsync(Vote vote, CancellationToken cancellationToken);
    Task RemoveAsync(Guid userId, Guid productId, CancellationToken cancellationToken);
}

/// <summary>
/// Abstraction over Kafka — infrastructure katmanında implement edilir.
/// Application katmanı transport'tan bağımsız kalır.
/// </summary>
public interface IVoteEventPublisher
{
    Task PublishVoteAddedAsync(VoteAddedEvent @event, CancellationToken cancellationToken = default);
    Task PublishVoteRemovedAsync(VoteRemovedEvent @event, CancellationToken cancellationToken = default);
}

public class AddVoteCommandHandler : IRequestHandler<AddVoteCommand, Result>
{
    private readonly IVoteRepository _repository;
    private readonly IVoteEventPublisher _eventPublisher;

    public AddVoteCommandHandler(IVoteRepository repository, IVoteEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result> Handle(AddVoteCommand request, CancellationToken cancellationToken)
    {
        var alreadyVoted = await _repository.HasUserVotedAsync(
            request.UserId, request.ProductId, cancellationToken);

        if (alreadyVoted)
            return Result.Failure("User has already voted for this product.");

        var vote = Vote.Create(request.UserId, request.ProductId);
        await _repository.AddAsync(vote, cancellationToken);

        // Kafka'ya event publish et → Product servisi consume edip kendi tablosunu günceller
        var @event = new VoteAddedEvent
        {
            VoteId    = vote.Id,
            UserId    = request.UserId,
            ProductId = request.ProductId
        };

        await _eventPublisher.PublishVoteAddedAsync(@event, cancellationToken);

        return Result.Success();
    }
}

/// <summary>
/// Oyun geri alınması (ileride kullanım için).
/// </summary>
public record RemoveVoteCommand(Guid UserId, Guid ProductId) : IRequest<Result>;

public class RemoveVoteCommandHandler : IRequestHandler<RemoveVoteCommand, Result>
{
    private readonly IVoteRepository _repository;
    private readonly IVoteEventPublisher _eventPublisher;

    public RemoveVoteCommandHandler(IVoteRepository repository, IVoteEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result> Handle(RemoveVoteCommand request, CancellationToken cancellationToken)
    {
        var hasVoted = await _repository.HasUserVotedAsync(
            request.UserId, request.ProductId, cancellationToken);

        if (!hasVoted)
            return Result.Failure("No vote found to remove.");

        await _repository.RemoveAsync(request.UserId, request.ProductId, cancellationToken);

        var @event = new VoteRemovedEvent
        {
            UserId    = request.UserId,
            ProductId = request.ProductId
        };

        await _eventPublisher.PublishVoteRemovedAsync(@event, cancellationToken);

        return Result.Success();
    }
}
