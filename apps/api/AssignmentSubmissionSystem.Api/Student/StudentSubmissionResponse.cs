using AssignmentSubmissionSystem.Domain.Submissions;

namespace AssignmentSubmissionSystem.Api.Student;

public sealed class StudentSubmissionResponse
{
    public string? AttachmentFileName { get; init; }

    public IReadOnlyList<SubmissionAttachmentResponse> Attachments { get; init; } = [];

    public string? Feedback { get; init; }

    public Guid Id { get; init; }

    public decimal? Marks { get; init; }

    public bool ResultsAvailable { get; init; }

    public string Status { get; init; } = string.Empty;

    public string? TextAnswer { get; init; }

    public DateTimeOffset SubmittedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public static StudentSubmissionResponse From(Submission submission, bool resultsAvailable = false)
    {
        return new StudentSubmissionResponse
        {
            AttachmentFileName = submission.Attachments.FirstOrDefault()?.FileName ?? submission.AttachmentFileName,
            Attachments = submission.Attachments.Select(attachment => new SubmissionAttachmentResponse { Id = attachment.Id, FileName = attachment.FileName }).ToList(),
            Feedback = resultsAvailable ? submission.Feedback : null,
            Marks = resultsAvailable ? submission.Marks : null,
            Id = submission.Id,
            ResultsAvailable = resultsAvailable,
            Status = submission.Status.ToString(),
            SubmittedAt = submission.SubmittedAt,
            TextAnswer = submission.TextAnswer,
            UpdatedAt = submission.UpdatedAt
        };
    }
}
