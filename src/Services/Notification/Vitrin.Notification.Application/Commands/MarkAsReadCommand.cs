using Vitrin.Shared.Kernel.Results;
using MediatR;

namespace Vitrin.Notification.Application.Commands;

public record MarkAsReadCommand(Guid NotificationId, Guid UserId) : IRequest<Result>;

public class MarkAsReadCommandHandler : IRequestHandler<MarkAsReadCommand, Result>
{
    private readonly INotificationRepository _repository;

    public MarkAsReadCommandHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _repository.GetByIdAsync(request.NotificationId, cancellationToken);
        if (notification == null)
            return Result.Failure("Notification not found.");

        if (notification.UserId != request.UserId)
            return Result.Failure("Unauthorized.");

        notification.MarkAsRead();
        await _repository.UpdateAsync(notification, cancellationToken);

        return Result.Success();
    }
}
