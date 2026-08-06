using Vitrin.Notification.Domain.Entities;
using Vitrin.Shared.Kernel.Results;
using MediatR;
using Vitrin.Notification.Application.Notifications;

namespace Vitrin.Notification.Application.Commands;

public record SendNotificationCommand(Guid UserId, string Message, string? NotificationType = null, Guid? RelatedEntityId = null) : IRequest<Result<Guid>>;

public interface INotificationRepository
{
    Task AddAsync(NotificationItem notification, CancellationToken cancellationToken);
    Task<NotificationPreference?> GetPreferenceAsync(Guid userId, CancellationToken cancellationToken);
    Task<NotificationItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(NotificationItem notification, CancellationToken cancellationToken);
}

public class SendNotificationCommandHandler : IRequestHandler<SendNotificationCommand, Result<Guid>>
{
    private readonly INotificationRepository _repository;
    private readonly INotificationRealtimePublisher _realtimePublisher;

    public SendNotificationCommandHandler(
        INotificationRepository repository,
        INotificationRealtimePublisher realtimePublisher)
    {
        _repository = repository;
        _realtimePublisher = realtimePublisher;
    }

    public async Task<Result<Guid>> Handle(SendNotificationCommand request, CancellationToken cancellationToken)
    {
        var preference = await _repository.GetPreferenceAsync(request.UserId, cancellationToken);
        if (preference is not null &&
            (!preference.AllowsType(request.NotificationType) ||
             (!preference.InAppEnabled && !preference.EmailEnabled)))
        {
            return Result<Guid>.Success(Guid.Empty);
        }

        var notificationResult = NotificationItem.Create(request.UserId, request.Message, request.NotificationType, request.RelatedEntityId);
        if (!notificationResult.IsSuccess)
            return Result<Guid>.Failure(notificationResult.Error);

        await _repository.AddAsync(notificationResult.Value, cancellationToken);

        if (preference?.InAppEnabled != false)
        {
            await _realtimePublisher.PublishAsync(
                RealtimeNotificationMapper.From(notificationResult.Value),
                cancellationToken);
        }
        
        return Result<Guid>.Success(notificationResult.Value.Id);
    }
}
