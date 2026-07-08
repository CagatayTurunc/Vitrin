using Vitrin.Comment.Application.Commands;
using Vitrin.Comment.Domain.Entities;
using Vitrin.Comment.Infrastructure.Data;

namespace Vitrin.Comment.Infrastructure.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly CommentDbContext _context;

    public CommentRepository(CommentDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(CommentItem comment, CancellationToken cancellationToken)
    {
        await _context.Comments.AddAsync(comment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
