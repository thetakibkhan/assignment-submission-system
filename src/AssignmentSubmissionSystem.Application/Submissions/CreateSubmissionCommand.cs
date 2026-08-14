namespace AssignmentSubmissionSystem.Application.Submissions;

public sealed class CreateSubmissionCommand
{
    public IReadOnlyList<SubmissionAttachmentUpload> Attachments { get; init; } = [];

    public IReadOnlySet<Guid> RemovedAttachmentIds { get; init; } = new HashSet<Guid>();

    public string? TextAnswer { get; init; }
}
