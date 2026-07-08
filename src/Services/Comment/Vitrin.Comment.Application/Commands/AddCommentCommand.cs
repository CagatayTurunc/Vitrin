using Vitrin.Comment.Domain.Entities;
using Vitrin.Shared.Kernel.Results;
using MediatR;

namespace Vitrin.Comment.Application.Commands;

public record AddCommentCommand(Guid ProductId, Guid UserId, string Content, Guid? ParentCommentId = null) : IRequest<Result<Guid>>;

public interface ICommentRepository
{
    Task AddAsync(CommentItem comment, CancellationToken cancellationToken);
}

public class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, Result<Guid>>
{
    private readonly ICommentRepository _repository;

    public AddCommentCommandHandler(ICommentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        var commentResult = CommentItem.Create(request.ProductId, request.UserId, request.Content, request.ParentCommentId);
        if (!commentResult.IsSuccess)
        {
            return Result<Guid>.Failure(commentResult.Error);
        }

        await _repository.AddAsync(commentResult.Value, cancellationToken);

        // Domain event publish edilebilir (örn. Product owner'a bildirim atmak için)
        
        return Result<Guid>.Success(commentResult.Value.Id);
    }
}
