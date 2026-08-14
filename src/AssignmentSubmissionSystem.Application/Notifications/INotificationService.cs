namespace AssignmentSubmissionSystem.Application.Notifications;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationItem>> GetForCurrentUserAsync(Guid recipientUserId, CancellationToken cancellationToken);
    Task MarkAllAsReadAsync(Guid recipientUserId, CancellationToken cancellationToken);
    Task<bool> MarkAsReadAsync(Guid notificationId, Guid recipientUserId, CancellationToken cancellationToken);
}
