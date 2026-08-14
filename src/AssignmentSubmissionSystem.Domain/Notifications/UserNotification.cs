namespace AssignmentSubmissionSystem.Domain.Notifications;

public sealed class UserNotification
{
    public UserNotification(
        Guid id,
        Guid recipientUserId,
        NotificationType type,
        Guid assignmentId,
        Guid? submissionId,
        string message,
        DateTimeOffset createdAt)
    {
        Id = id;
        RecipientUserId = recipientUserId;
        Type = type;
        AssignmentId = assignmentId;
        SubmissionId = submissionId;
        Message = message;
        CreatedAt = createdAt;
    }

    private UserNotification()
    {
    }

    public Guid Id { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public NotificationType Type { get; private set; }
    public Guid AssignmentId { get; private set; }
    public Guid? SubmissionId { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }

    public void MarkAsRead(DateTimeOffset readAt)
    {
        ReadAt ??= readAt;
    }
}
