namespace AssignmentSubmissionSystem.Domain.Submissions;

public sealed class SubmissionRevision
{
    public SubmissionRevision(
        Guid id,
        Guid submissionId,
        string? textAnswer,
        string? attachmentFileName,
        string? attachmentContentType,
        string? attachmentStorageName,
        DateTimeOffset recordedAt)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(submissionId, Guid.Empty);

        Id = id;
        SubmissionId = submissionId;
        TextAnswer = textAnswer;
        AttachmentFileName = attachmentFileName;
        AttachmentContentType = attachmentContentType;
        AttachmentStorageName = attachmentStorageName;
        RecordedAt = recordedAt;
    }

    public string? AttachmentContentType { get; private set; }

    public string? AttachmentFileName { get; private set; }

    public string? AttachmentStorageName { get; private set; }

    public Guid Id { get; private set; }

    public DateTimeOffset RecordedAt { get; private set; }

    public Guid SubmissionId { get; private set; }

    public string? TextAnswer { get; private set; }
}
