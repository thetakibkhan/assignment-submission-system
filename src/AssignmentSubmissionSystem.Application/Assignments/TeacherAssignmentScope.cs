namespace AssignmentSubmissionSystem.Application.Assignments;

public sealed class TeacherAssignmentScope
{
    public Guid ClassCourseId { get; init; }
    public string ClassCourseName { get; init; } = string.Empty;
    public Guid SubjectId { get; init; }
    public string SubjectName { get; init; } = string.Empty;
}
