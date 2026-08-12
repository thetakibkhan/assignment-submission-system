namespace AssignmentSubmissionSystem.Domain.Submissions;

public sealed class Submission
{
    public Submission(
        Guid id,
        Guid assignmentId,
        Guid studentUserId,
        string? textAnswer,
        DateTimeOffset submittedAt,
        string? attachmentFileName = null,
        string? attachmentContentType = null,
        string? attachmentStorageName = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(assignmentId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(studentUserId, Guid.Empty);

        TextAnswer = NormalizeTextAnswer(textAnswer);
        if (TextAnswer is null && string.IsNullOrWhiteSpace(attachmentStorageName))
        {
            throw new ArgumentException("A text answer or attachment is required.", nameof(textAnswer));
        }
        Id = id;
        AssignmentId = assignmentId;
        StudentUserId = studentUserId;
        SubmittedAt = submittedAt;
        UpdatedAt = submittedAt;
        AttachmentContentType = attachmentContentType;
        AttachmentFileName = attachmentFileName;
        AttachmentStorageName = attachmentStorageName;
        Status = SubmissionStatus.Submitted;
    }

    public Guid Id { get; private set; }

    public Guid AssignmentId { get; private set; }

    public Guid StudentUserId { get; private set; }

    public string? AttachmentContentType { get; private set; }

    public string? AttachmentFileName { get; private set; }

    public string? AttachmentStorageName { get; private set; }

    public SubmissionStatus Status { get; private set; }

    public string? TextAnswer { get; private set; }

    public DateTimeOffset SubmittedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public SubmissionRevision CreateRevision(Guid revisionId, DateTimeOffset recordedAt)
    {
        return new SubmissionRevision(
            revisionId,
            Id,
            TextAnswer,
            AttachmentFileName,
            AttachmentContentType,
            AttachmentStorageName,
            recordedAt);
    }

    public void UpdateContent(
        string? textAnswer,
        string? attachmentFileName,
        string? attachmentContentType,
        string? attachmentStorageName,
        DateTimeOffset updatedAt)
    {
        if (Status != SubmissionStatus.Submitted)
        {
            throw new InvalidOperationException("Only a submitted submission can be updated.");
        }

        string? normalizedTextAnswer = NormalizeTextAnswer(textAnswer);
        string? effectiveStorageName = attachmentStorageName ?? AttachmentStorageName;
        if (normalizedTextAnswer is null && string.IsNullOrWhiteSpace(effectiveStorageName))
        {
            throw new ArgumentException("A text answer or attachment is required.", nameof(textAnswer));
        }

        TextAnswer = normalizedTextAnswer;
        if (attachmentStorageName is not null)
        {
            AttachmentContentType = attachmentContentType;
            AttachmentFileName = attachmentFileName;
            AttachmentStorageName = attachmentStorageName;
        }
        UpdatedAt = updatedAt;
    }

    private static string? NormalizeTextAnswer(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
