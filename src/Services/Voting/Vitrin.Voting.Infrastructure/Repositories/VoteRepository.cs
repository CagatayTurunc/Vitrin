using Microsoft.EntityFrameworkCore;
using Vitrin.Voting.Application.Commands;
using Vitrin.Voting.Domain.Entities;
using Vitrin.Voting.Infrastructure.Data;

namespace Vitrin.Voting.Infrastructure.Repositories;

public class VoteRepository : IVoteRepository
{
    private readonly VoteDbContext _context;

    public VoteRepository(VoteDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasUserVotedAsync(Guid userId, Guid productId, CancellationToken cancellationToken)
    {
        return await _context.Votes
            .AnyAsync(v => v.UserId == userId && v.ProductId == productId, cancellationToken);
    }

    public async Task AddAsync(Vote vote, CancellationToken cancellationToken)
    {
        await _context.Votes.AddAsync(vote, cancellationToken);
    }

    public async Task RemoveAsync(Guid userId, Guid productId, CancellationToken cancellationToken)
    {
        var vote = await _context.Votes
            .FirstOrDefaultAsync(v => v.UserId == userId && v.ProductId == productId, cancellationToken);

        if (vote is not null)
        {
            _context.Votes.Remove(vote);
        }
    }
}
