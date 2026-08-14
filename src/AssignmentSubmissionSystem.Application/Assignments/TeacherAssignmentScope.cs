namespace AssignmentSubmissionSystem.Application.Assignments;

public sealed class TeacherAssignmentScope
{
    public Guid AcademicClassId { get; init; }
    public string AcademicClassName { get; init; } = string.Empty;
    public Guid SubjectId { get; init; }
    public string SubjectName { get; init; } = string.Empty;
}
