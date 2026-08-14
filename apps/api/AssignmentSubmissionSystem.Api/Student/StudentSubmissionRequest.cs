namespace AssignmentSubmissionSystem.Api.Student;

public sealed class StudentSubmissionRequest
{
    public IFormFile? Attachment { get; init; }

    public List<IFormFile> Attachments { get; init; } = [];

    public List<Guid> RemovedAttachmentIds { get; init; } = [];

    public string? TextAnswer { get; init; }
}
