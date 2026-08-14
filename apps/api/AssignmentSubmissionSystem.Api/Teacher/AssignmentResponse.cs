using AssignmentSubmissionSystem.Domain.Assignments;

namespace AssignmentSubmissionSystem.Api.Teacher;

public sealed class AssignmentResponse
{
    public Guid Id { get; init; }
    public Guid AcademicClassId { get; init; }
    public Guid SubjectId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTimeOffset? Deadline { get; init; }
    public decimal? MaximumMarks { get; init; }
    public bool? AllowSubmissionUpdates { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool CanReturnToDraft { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }

    public static AssignmentResponse From(Assignment assignment, bool canReturnToDraft = false) => new()
    {
        Id = assignment.Id,
        AcademicClassId = assignment.AcademicClassId,
        SubjectId = assignment.SubjectId,
        Title = assignment.Title,
        Description = assignment.Description,
        Deadline = assignment.Deadline,
        MaximumMarks = assignment.MaximumMarks,
        AllowSubmissionUpdates = assignment.AllowSubmissionUpdates,
        Status = assignment.Status.ToString(),
        CanReturnToDraft = assignment.Status == AssignmentStatus.Published && canReturnToDraft,
        UpdatedAt = assignment.UpdatedAt
    };
}
