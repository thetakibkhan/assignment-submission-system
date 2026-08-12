using AssignmentSubmissionSystem.Domain.Assignments;

namespace AssignmentSubmissionSystem.Api.Teacher;

public sealed class AssignmentResponse
{
    public Guid Id { get; init; }
    public Guid ClassCourseId { get; init; }
    public Guid SubjectId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTimeOffset? Deadline { get; init; }
    public decimal? MaximumMarks { get; init; }
    public bool? AllowSubmissionUpdates { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; init; }
    public static AssignmentResponse From(Assignment assignment) => new() { Id = assignment.Id, ClassCourseId = assignment.ClassCourseId, SubjectId = assignment.SubjectId, Title = assignment.Title, Description = assignment.Description, Deadline = assignment.Deadline, MaximumMarks = assignment.MaximumMarks, AllowSubmissionUpdates = assignment.AllowSubmissionUpdates, Status = assignment.Status.ToString(), UpdatedAt = assignment.UpdatedAt };
}
