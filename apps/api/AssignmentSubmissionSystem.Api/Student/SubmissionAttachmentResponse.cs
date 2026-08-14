namespace AssignmentSubmissionSystem.Api.Student;

public sealed class SubmissionAttachmentResponse
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = string.Empty;
}
