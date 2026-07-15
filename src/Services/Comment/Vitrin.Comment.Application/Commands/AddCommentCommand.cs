using MediatR;
using Vitrin.Comment.Domain.Entities;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Comment.Application.Commands;

public record AddCommentCommand(
    Guid ProductId,
    Guid UserId,
    string UserName,
    string Content,
    Guid? ParentCommentId = null,
    Guid? ProductMakerId = null)   // caller biliyorsa geçer, consumer'dan gelmeyebilir
    : IRequest<Result<Guid>>;

public interface ICommentRepository
{
    Task<CommentItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(CommentItem comment, CancellationToken cancellationToken);
    Task UpdateAsync(CommentItem comment, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Bildirim göndermek için abstraction — infrastructure katmanı Kafka ile implement eder.
/// </summary>
public interface ICommentNotificationPublisher
{
    Task NotifyAsync(Guid recipientUserId, string message, string notificationType, CancellationToken ct = default);
}

public class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, Result<Guid>>
{
    private readonly ICommentRepository _repository;
    private readonly ICommentNotificationPublisher _notificationPublisher;

    public AddCommentCommandHandler(
        ICommentRepository repository,
        ICommentNotificationPublisher notificationPublisher)
    {
        _repository              = repository;
        _notificationPublisher   = notificationPublisher;
    }

    public async Task<Result<Guid>> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        var commentResult = CommentItem.Create(
            request.ProductId,
            request.UserId,
            request.UserName,
            request.Content,
            request.ParentCommentId);

        if (!commentResult.IsSuccess)
            return Result<Guid>.Failure(commentResult.Error);

        await _repository.AddAsync(commentResult.Value, cancellationToken);

        // 1. Üst yoruma cevap verilmişse → üst yorum sahibine bildirim
        if (request.ParentCommentId.HasValue)
        {
            var parent = await _repository.GetByIdAsync(request.ParentCommentId.Value, cancellationToken);
            if (parent is not null && parent.UserId != request.UserId)
            {
                await _notificationPublisher.NotifyAsync(
                    parent.UserId,
                    $"@{request.UserName} yorumunuza cevap verdi.",
                    "comment_reply",
                    cancellationToken);
            }
        }

        // 2. Ürün sahibine (maker) bildirim — MakerId biliniyorsa
        if (request.ProductMakerId.HasValue && request.ProductMakerId.Value != request.UserId)
        {
            await _notificationPublisher.NotifyAsync(
                request.ProductMakerId.Value,
                $"@{request.UserName} ürününüze yorum yaptı.",
                "comment_on_product",
                cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(commentResult.Value.Id);
    }
}
