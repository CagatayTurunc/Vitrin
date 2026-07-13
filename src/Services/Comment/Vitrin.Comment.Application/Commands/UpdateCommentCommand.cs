using Vitrin.Comment.Domain.Entities;
using Vitrin.Shared.Kernel.Results;
using MediatR;

namespace Vitrin.Comment.Application.Commands;

public record UpdateCommentCommand(Guid CommentId, Guid UserId, string Content) : IRequest<Result<bool>>;

public class UpdateCommentCommandHandler : IRequestHandler<UpdateCommentCommand, Result<bool>>
{
    private readonly ICommentRepository _repository;

    public UpdateCommentCommandHandler(ICommentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<bool>> Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await _repository.GetByIdAsync(request.CommentId, cancellationToken);
        if (comment == null)
            return Result<bool>.Failure("Comment not found.");

        if (comment.UserId != request.UserId)
            return Result<bool>.Failure("Unauthorized to update this comment.");

        var updateResult = comment.UpdateContent(request.Content);
        if (!updateResult.IsSuccess)
            return Result<bool>.Failure(updateResult.Error);

        await _repository.UpdateAsync(comment, cancellationToken);
        return Result<bool>.Success(true);
    }
}
