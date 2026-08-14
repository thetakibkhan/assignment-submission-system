using AssignmentSubmissionSystem.Domain.Notifications;

namespace AssignmentSubmissionSystem.Application.Notifications;

public sealed class NotificationItem
{
    public Guid AssignmentId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public Guid Id { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool IsRead { get; init; }
    public Guid? SubmissionId { get; init; }
    public NotificationType Type { get; init; }
}
