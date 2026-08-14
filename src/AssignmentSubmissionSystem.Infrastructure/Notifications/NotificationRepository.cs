using AssignmentSubmissionSystem.Application.Notifications;
using AssignmentSubmissionSystem.Domain.Notifications;
using AssignmentSubmissionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Infrastructure.Notifications;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _databaseContext;

    public NotificationRepository(ApplicationDbContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public async Task DeleteAsync(UserNotification notification, CancellationToken cancellationToken)
    {
        _databaseContext.UserNotifications.Remove(notification);
        await _databaseContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserNotification>> GetForRecipientAsync(Guid recipientUserId, CancellationToken cancellationToken)
    {
        return await _databaseContext.UserNotifications.AsNoTracking()
            .Where(notification => notification.RecipientUserId == recipientUserId)
            .OrderByDescending(notification => notification.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<UserNotification?> GetForRecipientAsync(Guid notificationId, Guid recipientUserId, CancellationToken cancellationToken)
        => _databaseContext.UserNotifications.SingleOrDefaultAsync(notification => notification.Id == notificationId && notification.RecipientUserId == recipientUserId, cancellationToken);

    public Task MarkAllAsReadAsync(Guid recipientUserId, DateTimeOffset readAt, CancellationToken cancellationToken)
        => _databaseContext.UserNotifications
            .Where(notification => notification.RecipientUserId == recipientUserId && notification.ReadAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(notification => notification.ReadAt, readAt), cancellationToken);

    public async Task UpdateAsync(UserNotification notification, CancellationToken cancellationToken)
    {
        _databaseContext.UserNotifications.Update(notification);
        await _databaseContext.SaveChangesAsync(cancellationToken);
    }
}
