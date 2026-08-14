namespace AssignmentSubmissionSystem.Application.AcademicSetup.Enrollments;

public sealed class CreateStudentEnrollmentCommand
{
    public Guid AcademicClassId { get; init; }

    public string StudentInstitutionalId { get; init; } = string.Empty;
}
