using Vitrin.Notification.Application.Commands;
using Vitrin.Notification.Domain.Entities;
using Vitrin.Notification.Infrastructure.Data;

namespace Vitrin.Notification.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _context;

    public NotificationRepository(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(NotificationItem notification, CancellationToken cancellationToken)
    {
        await _context.Notifications.AddAsync(notification, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<NotificationItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Notifications.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task UpdateAsync(NotificationItem notification, CancellationToken cancellationToken)
    {
        _context.Notifications.Update(notification);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
