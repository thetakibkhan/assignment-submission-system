namespace AssignmentSubmissionSystem.Api.Student;

public sealed class StudentSubmissionRequest
{
    public IFormFile? Attachment { get; init; }

    public string? TextAnswer { get; init; }
}
