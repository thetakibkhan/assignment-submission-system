namespace AssignmentSubmissionSystem.Domain.Submissions;

public sealed class Submission
{
    public Submission(
        Guid id,
        Guid assignmentId,
        Guid studentUserId,
        string? textAnswer,
        DateTimeOffset submittedAt)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(assignmentId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(studentUserId, Guid.Empty);

        TextAnswer = NormalizeTextAnswer(textAnswer)
            ?? throw new ArgumentException("A text answer or attachment is required.", nameof(textAnswer));
        Id = id;
        AssignmentId = assignmentId;
        StudentUserId = studentUserId;
        SubmittedAt = submittedAt;
        UpdatedAt = submittedAt;
        Status = SubmissionStatus.Submitted;
    }

    public Guid Id { get; private set; }

    public Guid AssignmentId { get; private set; }

    public Guid StudentUserId { get; private set; }

    public SubmissionStatus Status { get; private set; }

    public string? TextAnswer { get; private set; }

    public DateTimeOffset SubmittedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void UpdateTextAnswer(string? textAnswer, DateTimeOffset updatedAt)
    {
        if (Status != SubmissionStatus.Submitted)
        {
            throw new InvalidOperationException("Only a submitted submission can be updated.");
        }

        TextAnswer = NormalizeTextAnswer(textAnswer)
            ?? throw new ArgumentException("A text answer or attachment is required.", nameof(textAnswer));
        UpdatedAt = updatedAt;
    }

    private static string? NormalizeTextAnswer(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
