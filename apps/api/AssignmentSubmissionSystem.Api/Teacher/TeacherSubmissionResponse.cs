using AssignmentSubmissionSystem.Application.Submissions;

namespace AssignmentSubmissionSystem.Api.Teacher;

public sealed class TeacherSubmissionResponse
{
    public string? AttachmentFileName { get; init; }

    public string? Feedback { get; init; }

    public Guid Id { get; init; }

    public decimal? Marks { get; init; }

    public string Status { get; init; } = string.Empty;

    public string StudentName { get; init; } = string.Empty;

    public DateTimeOffset SubmittedAt { get; init; }

    public string? TextAnswer { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public static TeacherSubmissionResponse From(TeacherSubmissionItem item)
    {
        return new TeacherSubmissionResponse
        {
            AttachmentFileName = item.AttachmentFileName,
            Feedback = item.Feedback,
            Id = item.Id,
            Marks = item.Marks,
            Status = item.Status.ToString(),
            StudentName = item.StudentName,
            SubmittedAt = item.SubmittedAt,
            TextAnswer = item.TextAnswer,
            UpdatedAt = item.UpdatedAt
        };
    }
}
