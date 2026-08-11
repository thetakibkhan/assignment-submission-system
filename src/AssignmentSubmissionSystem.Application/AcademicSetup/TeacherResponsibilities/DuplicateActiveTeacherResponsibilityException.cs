namespace AssignmentSubmissionSystem.Application.AcademicSetup.TeacherResponsibilities;

public sealed class DuplicateActiveTeacherResponsibilityException : Exception
{
    public DuplicateActiveTeacherResponsibilityException()
        : base("This Class/Course and Subject combination already has an active Teacher responsibility.")
    {
    }
}
