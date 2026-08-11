namespace AssignmentSubmissionSystem.Application.AcademicSetup.Enrollments;

public sealed class CreateStudentEnrollmentCommand
{
    public Guid ClassCourseId { get; init; }

    public string StudentInstitutionalId { get; init; } = string.Empty;
}
