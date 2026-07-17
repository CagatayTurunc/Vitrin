using Vitrin.Notification.Domain.Entities;
using Vitrin.Shared.Kernel.Results;
using MediatR;

namespace Vitrin.Notification.Application.Commands;

public record SendNotificationCommand(Guid UserId, string Message, string? NotificationType = null) : IRequest<Result<Guid>>;

public interface INotificationRepository
{
    Task AddAsync(NotificationItem notification, CancellationToken cancellationToken);
    Task<NotificationItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(NotificationItem notification, CancellationToken cancellationToken);
}

public class SendNotificationCommandHandler : IRequestHandler<SendNotificationCommand, Result<Guid>>
{
    private readonly INotificationRepository _repository;

    public SendNotificationCommandHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(SendNotificationCommand request, CancellationToken cancellationToken)
    {
        var notificationResult = NotificationItem.Create(request.UserId, request.Message, request.NotificationType);
        if (!notificationResult.IsSuccess)
            return Result<Guid>.Failure(notificationResult.Error);

        await _repository.AddAsync(notificationResult.Value, cancellationToken);
        
        return Result<Guid>.Success(notificationResult.Value.Id);
    }
}
