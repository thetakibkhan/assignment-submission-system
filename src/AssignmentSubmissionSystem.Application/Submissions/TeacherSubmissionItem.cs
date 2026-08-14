using AssignmentSubmissionSystem.Domain.Submissions;

namespace AssignmentSubmissionSystem.Application.Submissions;

public sealed class TeacherSubmissionItem
{
    public string? AttachmentFileName => Attachments.FirstOrDefault()?.FileName;

    public IReadOnlyList<SubmissionAttachmentItem> Attachments { get; init; } = [];

    public string? Feedback { get; init; }

    public Guid Id { get; init; }

    public decimal? Marks { get; init; }

    public SubmissionStatus Status { get; init; }

    public Guid StudentUserId { get; init; }

    public string StudentName { get; init; } = string.Empty;

    public DateTimeOffset SubmittedAt { get; init; }

    public string? TextAnswer { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}
