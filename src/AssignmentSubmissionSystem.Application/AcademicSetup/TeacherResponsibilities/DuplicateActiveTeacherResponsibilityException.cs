namespace AssignmentSubmissionSystem.Application.AcademicSetup.TeacherResponsibilities;

public sealed class DuplicateActiveTeacherResponsibilityException : Exception
{
    public DuplicateActiveTeacherResponsibilityException()
        : base("This Class and Subject combination already has an active Teacher responsibility.")
    {
    }
}
