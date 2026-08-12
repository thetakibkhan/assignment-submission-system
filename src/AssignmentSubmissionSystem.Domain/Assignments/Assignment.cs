namespace AssignmentSubmissionSystem.Domain.Assignments;

public sealed class Assignment
{
    public Assignment(
        Guid id,
        Guid teacherUserId,
        Guid classCourseId,
        Guid subjectId,
        string title,
        string? description,
        DateTimeOffset? deadline,
        decimal? maximumMarks,
        bool? allowSubmissionUpdates,
        DateTimeOffset createdAt)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(teacherUserId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(classCourseId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(subjectId, Guid.Empty);

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("A title is required for an assignment.", nameof(title));
        }

        if (maximumMarks is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumMarks), "Maximum marks must be positive when provided.");
        }

        Id = id;
        TeacherUserId = teacherUserId;
        ClassCourseId = classCourseId;
        SubjectId = subjectId;
        Title = title.Trim();
        Description = NormalizeOptionalText(description);
        Deadline = deadline;
        MaximumMarks = maximumMarks;
        AllowSubmissionUpdates = allowSubmissionUpdates;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        Status = AssignmentStatus.Draft;
    }

    public Guid Id { get; private set; }

    public Guid TeacherUserId { get; private set; }

    public Guid ClassCourseId { get; private set; }

    public Guid SubjectId { get; private set; }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    public DateTimeOffset? Deadline { get; private set; }

    public decimal? MaximumMarks { get; private set; }

    public bool? AllowSubmissionUpdates { get; private set; }

    public AssignmentStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public void Publish(DateTimeOffset publishedAt)
    {
        if (Status != AssignmentStatus.Draft)
        {
            throw new InvalidOperationException("Only a Draft assignment can be published.");
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            throw new InvalidOperationException("A description is required before publishing an assignment.");
        }

        if (Deadline is null || Deadline <= publishedAt)
        {
            throw new InvalidOperationException("A future deadline is required before publishing an assignment.");
        }

        if (MaximumMarks is null || MaximumMarks <= 0)
        {
            throw new InvalidOperationException("Positive maximum marks are required before publishing an assignment.");
        }

        if (AllowSubmissionUpdates is null)
        {
            throw new InvalidOperationException("The submission update policy must be selected before publishing an assignment.");
        }

        Status = AssignmentStatus.Published;
        PublishedAt = publishedAt;
        UpdatedAt = publishedAt;
    }

    public void Unpublish(bool hasSubmissions)
    {
        if (Status != AssignmentStatus.Published)
        {
            throw new InvalidOperationException("Only a Published assignment can return to Draft.");
        }

        if (hasSubmissions)
        {
            throw new InvalidOperationException("An assignment with submissions cannot return to Draft.");
        }

        Status = AssignmentStatus.Draft;
        PublishedAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(
        Guid classCourseId,
        Guid subjectId,
        string title,
        string? description,
        DateTimeOffset? deadline,
        decimal? maximumMarks,
        bool? allowSubmissionUpdates,
        bool hasSubmissions,
        DateTimeOffset updatedAt)
    {
        if (Status == AssignmentStatus.Published)
        {
            throw new InvalidOperationException("A Published assignment must return to Draft before it can be edited.");
        }

        if (hasSubmissions)
        {
            throw new InvalidOperationException("An assignment with submissions cannot be edited through the standard workflow.");
        }

        ArgumentOutOfRangeException.ThrowIfEqual(classCourseId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(subjectId, Guid.Empty);

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("A title is required for an assignment.", nameof(title));
        }

        if (maximumMarks is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumMarks), "Maximum marks must be positive when provided.");
        }

        ClassCourseId = classCourseId;
        SubjectId = subjectId;
        Title = title.Trim();
        Description = NormalizeOptionalText(description);
        Deadline = deadline;
        MaximumMarks = maximumMarks;
        AllowSubmissionUpdates = allowSubmissionUpdates;
        UpdatedAt = updatedAt;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
