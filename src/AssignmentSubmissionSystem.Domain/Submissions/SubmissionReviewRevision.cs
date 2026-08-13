namespace AssignmentSubmissionSystem.Domain.Submissions;

public sealed class SubmissionReviewRevision
{
    public SubmissionReviewRevision(
        Guid id,
        Guid submissionId,
        SubmissionStatus status,
        decimal? marks,
        string? feedback,
        DateTimeOffset recordedAt)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(submissionId, Guid.Empty);

        Id = id;
        SubmissionId = submissionId;
        Status = status;
        Marks = marks;
        Feedback = feedback;
        RecordedAt = recordedAt;
    }

    public string? Feedback { get; private set; }

    public Guid Id { get; private set; }

    public decimal? Marks { get; private set; }

    public DateTimeOffset RecordedAt { get; private set; }

    public Guid SubmissionId { get; private set; }

    public SubmissionStatus Status { get; private set; }
}
