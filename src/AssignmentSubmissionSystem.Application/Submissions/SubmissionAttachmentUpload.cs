namespace AssignmentSubmissionSystem.Application.Submissions;

public sealed class SubmissionAttachmentUpload
{
    public required Stream Content { get; init; }

    public required string ContentType { get; init; }

    public required string OriginalFileName { get; init; }
}
