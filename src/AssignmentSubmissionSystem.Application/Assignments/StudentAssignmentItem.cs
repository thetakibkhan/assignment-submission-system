namespace AssignmentSubmissionSystem.Application.Assignments;

public sealed class StudentAssignmentItem
{
    public bool AllowSubmissionUpdates { get; init; }
    public string AcademicClassName { get; init; } = string.Empty;
    public DateTimeOffset Deadline { get; init; }
    public bool DeadlinePassed { get; init; }
    public string Description { get; init; } = string.Empty;
    public Guid Id { get; init; }
    public decimal MaximumMarks { get; init; }
    public string StudentState { get; init; } = "Not submitted";
    public StudentSubmissionSummary? Submission { get; init; }
    public string SubjectName { get; init; } = string.Empty;
    public string TeacherName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
}

public sealed class StudentSubmissionSummary
{
    public string Status { get; init; } = string.Empty;
}
