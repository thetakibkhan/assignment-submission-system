namespace AssignmentSubmissionSystem.Application.Notifications;

public sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationService(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<IReadOnlyList<NotificationItem>> GetForCurrentUserAsync(Guid recipientUserId, CancellationToken cancellationToken)
    {
        return (await _notificationRepository.GetForRecipientAsync(recipientUserId, cancellationToken))
            .Select(notification => new NotificationItem
            {
                AssignmentId = notification.AssignmentId,
                CreatedAt = notification.CreatedAt,
                Id = notification.Id,
                IsRead = notification.ReadAt is not null,
                Message = notification.Message,
                SubmissionId = notification.SubmissionId,
                Type = notification.Type
            })
            .ToList();
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid recipientUserId, CancellationToken cancellationToken)
    {
        var notification = await _notificationRepository.GetForRecipientAsync(notificationId, recipientUserId, cancellationToken);
        if (notification is null)
        {
            return false;
        }

        notification.MarkAsRead(DateTimeOffset.UtcNow);
        await _notificationRepository.UpdateAsync(notification, cancellationToken);
        return true;
    }

    public Task MarkAllAsReadAsync(Guid recipientUserId, CancellationToken cancellationToken)
        => _notificationRepository.MarkAllAsReadAsync(recipientUserId, DateTimeOffset.UtcNow, cancellationToken);
}
