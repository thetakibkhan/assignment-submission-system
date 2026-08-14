namespace AssignmentSubmissionSystem.Application.AcademicSetup.Enrollments;

public sealed class DuplicateActiveEnrollmentException : Exception
{
    public DuplicateActiveEnrollmentException()
        : base("The student is already actively enrolled in this Class.")
    {
    }
}
