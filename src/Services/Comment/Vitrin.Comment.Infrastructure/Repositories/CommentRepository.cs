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

    public async Task<CommentItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Comments.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task UpdateAsync(CommentItem comment, CancellationToken cancellationToken)
    {
        _context.Comments.Update(comment);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
