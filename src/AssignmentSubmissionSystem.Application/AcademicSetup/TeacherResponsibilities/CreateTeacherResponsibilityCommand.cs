namespace AssignmentSubmissionSystem.Application.AcademicSetup.TeacherResponsibilities;

public sealed class CreateTeacherResponsibilityCommand
{
    public Guid ClassCourseId { get; init; }

    public Guid SubjectId { get; init; }

    public string TeacherInstitutionalId { get; init; } = string.Empty;
}
