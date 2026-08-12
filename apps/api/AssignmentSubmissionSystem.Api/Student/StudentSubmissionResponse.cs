using AssignmentSubmissionSystem.Domain.Submissions;

namespace AssignmentSubmissionSystem.Api.Student;

public sealed class StudentSubmissionResponse
{
    public Guid Id { get; init; }

    public string Status { get; init; } = string.Empty;

    public string? TextAnswer { get; init; }

    public DateTimeOffset SubmittedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public static StudentSubmissionResponse From(Submission submission)
    {
        return new StudentSubmissionResponse
        {
            Id = submission.Id,
            Status = submission.Status.ToString(),
            SubmittedAt = submission.SubmittedAt,
            TextAnswer = submission.TextAnswer,
            UpdatedAt = submission.UpdatedAt
        };
    }
}
