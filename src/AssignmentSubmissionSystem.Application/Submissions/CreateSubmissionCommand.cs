namespace AssignmentSubmissionSystem.Application.Submissions;

public sealed class CreateSubmissionCommand
{
    public SubmissionAttachmentUpload? Attachment { get; init; }

    public string? TextAnswer { get; init; }
}
