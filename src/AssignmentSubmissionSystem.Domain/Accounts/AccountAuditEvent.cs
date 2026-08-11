namespace AssignmentSubmissionSystem.Domain.Accounts;

public sealed class AccountAuditEvent
{
    public AccountAuditEvent(
        Guid id,
        Guid actorUserId,
        Guid targetUserId,
        AccountAuditEventType eventType,
        string changeSummary,
        DateTimeOffset occurredAt)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(actorUserId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(targetUserId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(changeSummary);

        Id = id;
        ActorUserId = actorUserId;
        TargetUserId = targetUserId;
        EventType = eventType;
        ChangeSummary = changeSummary.Trim();
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }

    public Guid ActorUserId { get; private set; }

    public Guid TargetUserId { get; private set; }

    public AccountAuditEventType EventType { get; private set; }

    public string ChangeSummary { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }
}
