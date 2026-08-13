using AssignmentSubmissionSystem.Domain.Submissions;

namespace AssignmentSubmissionSystem.Api.Student;

public sealed class StudentSubmissionResponse
{
    public string? AttachmentFileName { get; init; }

    public string? Feedback { get; init; }

    public Guid Id { get; init; }

    public decimal? Marks { get; init; }

    public string Status { get; init; } = string.Empty;

    public string? TextAnswer { get; init; }

    public DateTimeOffset SubmittedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public static StudentSubmissionResponse From(Submission submission)
    {
        return new StudentSubmissionResponse
        {
            AttachmentFileName = submission.AttachmentFileName,
            Feedback = submission.Status == SubmissionStatus.Graded ? submission.Feedback : null,
            Marks = submission.Status == SubmissionStatus.Graded ? submission.Marks : null,
            Id = submission.Id,
            Status = submission.Status.ToString(),
            SubmittedAt = submission.SubmittedAt,
            TextAnswer = submission.TextAnswer,
            UpdatedAt = submission.UpdatedAt
        };
    }
}
