using AssignmentSubmissionSystem.Domain.Notifications;

namespace AssignmentSubmissionSystem.Application.Notifications;

public interface INotificationRepository
{
    Task<IReadOnlyList<UserNotification>> GetForRecipientAsync(Guid recipientUserId, CancellationToken cancellationToken);
    Task<UserNotification?> GetForRecipientAsync(Guid notificationId, Guid recipientUserId, CancellationToken cancellationToken);
    Task MarkAllAsReadAsync(Guid recipientUserId, DateTimeOffset readAt, CancellationToken cancellationToken);
    Task UpdateAsync(UserNotification notification, CancellationToken cancellationToken);
}
