namespace AssignmentSubmissionSystem.Api.Teacher;

public sealed class CreateAssignmentRequest
{
    public Guid ClassCourseId { get; init; }
    public Guid SubjectId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTimeOffset? Deadline { get; init; }
    public decimal? MaximumMarks { get; init; }
    public bool? AllowSubmissionUpdates { get; init; }
}
