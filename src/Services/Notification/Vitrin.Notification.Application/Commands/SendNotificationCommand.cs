using Vitrin.Notification.Domain.Entities;
using Vitrin.Shared.Kernel.Results;
using MediatR;

namespace Vitrin.Notification.Application.Commands;

public record SendNotificationCommand(Guid UserId, string Message) : IRequest<Result<Guid>>;

public interface INotificationRepository
{
    Task AddAsync(NotificationItem notification, CancellationToken cancellationToken);
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
        var notificationResult = NotificationItem.Create(request.UserId, request.Message);
        if (!notificationResult.IsSuccess)
            return Result<Guid>.Failure(notificationResult.Error);

        await _repository.AddAsync(notificationResult.Value, cancellationToken);
        
        // SignalR ile anlık bildirim gönderme tetiklenebilir
        
        return Result<Guid>.Success(notificationResult.Value.Id);
    }
}
