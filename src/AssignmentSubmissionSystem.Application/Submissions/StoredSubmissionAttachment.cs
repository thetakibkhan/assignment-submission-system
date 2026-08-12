namespace AssignmentSubmissionSystem.Application.Submissions;

public sealed class StoredSubmissionAttachment
{
    public required string ContentType { get; init; }

    public required string OriginalFileName { get; init; }

    public required string StorageName { get; init; }
}
