using Vitrin.Comment.Domain.Entities;
using Vitrin.Shared.Kernel.Results;
using MediatR;

namespace Vitrin.Comment.Application.Commands;

public record DeleteCommentCommand(Guid CommentId, Guid UserId) : IRequest<Result<bool>>;

public class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand, Result<bool>>
{
    private readonly ICommentRepository _repository;

    public DeleteCommentCommandHandler(ICommentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<bool>> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await _repository.GetByIdAsync(request.CommentId, cancellationToken);
        if (comment == null)
            return Result<bool>.Failure("Comment not found.");

        if (comment.UserId != request.UserId)
            return Result<bool>.Failure("Unauthorized to delete this comment.");

        comment.MarkAsDeleted();

        await _repository.UpdateAsync(comment, cancellationToken);
        return Result<bool>.Success(true);
    }
}
