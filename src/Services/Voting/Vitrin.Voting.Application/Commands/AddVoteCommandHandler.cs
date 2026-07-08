using MediatR;
using Vitrin.Shared.Kernel.Results;
using Vitrin.Voting.Domain.Entities;

namespace Vitrin.Voting.Application.Commands;

public interface IVoteRepository
{
    Task<bool> HasUserVotedAsync(Guid userId, Guid productId, CancellationToken cancellationToken);
    Task AddAsync(Vote vote, CancellationToken cancellationToken);
}

public class AddVoteCommandHandler : IRequestHandler<AddVoteCommand, Result>
{
    private readonly IVoteRepository _repository;

    public AddVoteCommandHandler(IVoteRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(AddVoteCommand request, CancellationToken cancellationToken)
    {
        var alreadyVoted = await _repository.HasUserVotedAsync(request.UserId, request.ProductId, cancellationToken);
        
        if (alreadyVoted)
        {
            return Result.Failure("User has already voted for this product.");
        }

        var vote = Vote.Create(request.UserId, request.ProductId);
        
        await _repository.AddAsync(vote, cancellationToken);
        
        // Publish VoteAddedEvent to Kafka to update Redis leaderboards in real-time
        
        return Result.Success();
    }
}
