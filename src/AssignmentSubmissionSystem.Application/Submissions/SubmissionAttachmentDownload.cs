namespace AssignmentSubmissionSystem.Application.Submissions;

public sealed class SubmissionAttachmentDownload
{
    public required Stream Content { get; init; }

    public required string ContentType { get; init; }

    public required string FileName { get; init; }
}
