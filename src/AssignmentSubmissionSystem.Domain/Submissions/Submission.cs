namespace AssignmentSubmissionSystem.Domain.Submissions;

public sealed class Submission
{
    private readonly List<SubmissionAttachment> _attachments = [];

    private Submission()
    {
    }
    public Submission(
        Guid id,
        Guid assignmentId,
        Guid studentUserId,
        string? textAnswer,
        DateTimeOffset submittedAt,
        string? attachmentFileName = null,
        string? attachmentContentType = null,
        string? attachmentStorageName = null,
        IEnumerable<SubmissionAttachment>? attachments = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(assignmentId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(studentUserId, Guid.Empty);

        TextAnswer = NormalizeTextAnswer(textAnswer);
        if (attachments is not null)
        {
            _attachments.AddRange(attachments);
        }

        if (TextAnswer is null && string.IsNullOrWhiteSpace(attachmentStorageName) && _attachments.Count == 0)
        {
            throw new ArgumentException("A text answer or attachment is required.", nameof(textAnswer));
        }
        Id = id;
        AssignmentId = assignmentId;
        StudentUserId = studentUserId;
        SubmittedAt = submittedAt;
        UpdatedAt = submittedAt;
        AttachmentContentType = attachmentContentType;
        AttachmentFileName = attachmentFileName;
        AttachmentStorageName = attachmentStorageName;
        Status = SubmissionStatus.Submitted;
    }

    public Guid Id { get; private set; }

    public Guid AssignmentId { get; private set; }

    public Guid StudentUserId { get; private set; }

    public IReadOnlyCollection<SubmissionAttachment> Attachments => _attachments;

    public string? AttachmentContentType { get; private set; }

    public string? AttachmentFileName { get; private set; }

    public string? AttachmentStorageName { get; private set; }

    public SubmissionStatus Status { get; private set; }

    public string? Feedback { get; private set; }

    public decimal? Marks { get; private set; }

    public string? TextAnswer { get; private set; }

    public DateTimeOffset SubmittedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public SubmissionRevision CreateRevision(Guid revisionId, DateTimeOffset recordedAt)
    {
        return new SubmissionRevision(
            revisionId,
            Id,
            TextAnswer,
            AttachmentFileName,
            AttachmentContentType,
            AttachmentStorageName,
            recordedAt);
    }

    public SubmissionReviewRevision CreateReviewRevision(Guid revisionId, DateTimeOffset recordedAt)
    {
        return new SubmissionReviewRevision(
            revisionId,
            Id,
            Status,
            Marks,
            Feedback,
            recordedAt);
    }

    public void StartReview()
    {
        if (Status != SubmissionStatus.Submitted)
        {
            throw new InvalidOperationException("Only submitted work can be started for review.");
        }

        Status = SubmissionStatus.UnderReview;
    }

    public void UpdateReview(decimal? marks, string? feedback)
    {
        if (Status != SubmissionStatus.UnderReview)
        {
            throw new InvalidOperationException("Only work under review can receive marks or feedback.");
        }

        Marks = marks;
        Feedback = string.IsNullOrWhiteSpace(feedback) ? null : feedback.Trim();
    }

    public void Grade()
    {
        if (Status != SubmissionStatus.UnderReview)
        {
            throw new InvalidOperationException("Only work under review can be graded.");
        }

        if (Marks is null)
        {
            throw new InvalidOperationException("Marks are required before grading.");
        }

        Status = SubmissionStatus.Graded;
    }

    public void ReopenForCorrection()
    {
        if (Status != SubmissionStatus.Graded)
        {
            throw new InvalidOperationException("Only graded work can be reopened for correction.");
        }

        Status = SubmissionStatus.UnderReview;
    }

    public void UpdateContent(
        string? textAnswer,
        string? attachmentFileName,
        string? attachmentContentType,
        string? attachmentStorageName,
        DateTimeOffset updatedAt)
    {
        if (Status != SubmissionStatus.Submitted)
        {
            throw new InvalidOperationException("Only a submitted submission can be updated.");
        }

        string? normalizedTextAnswer = NormalizeTextAnswer(textAnswer);
        string? effectiveStorageName = attachmentStorageName ?? AttachmentStorageName;
        if (normalizedTextAnswer is null && string.IsNullOrWhiteSpace(effectiveStorageName))
        {
            throw new ArgumentException("A text answer or attachment is required.", nameof(textAnswer));
        }

        TextAnswer = normalizedTextAnswer;
        if (attachmentStorageName is not null)
        {
            AttachmentContentType = attachmentContentType;
            AttachmentFileName = attachmentFileName;
            AttachmentStorageName = attachmentStorageName;
        }
        UpdatedAt = updatedAt;
    }

    public void UpdateContent(
        string? textAnswer,
        IReadOnlySet<Guid> removedAttachmentIds,
        IEnumerable<SubmissionAttachment> newAttachments,
        DateTimeOffset updatedAt)
    {
        if (Status != SubmissionStatus.Submitted)
        {
            throw new InvalidOperationException("Only a submitted submission can be updated.");
        }

        string? normalizedTextAnswer = NormalizeTextAnswer(textAnswer);
        List<SubmissionAttachment> additions = newAttachments.ToList();
        int remainingAttachmentCount = _attachments.Count(attachment => !removedAttachmentIds.Contains(attachment.Id));
        if (normalizedTextAnswer is null && remainingAttachmentCount + additions.Count == 0)
        {
            throw new ArgumentException("A text answer or attachment is required.", nameof(textAnswer));
        }

        TextAnswer = normalizedTextAnswer;
        _attachments.RemoveAll(attachment => removedAttachmentIds.Contains(attachment.Id));
        _attachments.AddRange(additions);
        AttachmentContentType = null;
        AttachmentFileName = null;
        AttachmentStorageName = null;
        UpdatedAt = updatedAt;
    }

    private static string? NormalizeTextAnswer(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
