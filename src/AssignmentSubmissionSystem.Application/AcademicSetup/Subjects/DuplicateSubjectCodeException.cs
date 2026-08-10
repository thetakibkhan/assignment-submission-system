namespace AssignmentSubmissionSystem.Application.AcademicSetup.Subjects;

public sealed class DuplicateSubjectCodeException : Exception
{
    public DuplicateSubjectCodeException(string code)
        : base($"A Subject with code {code} already exists.")
    {
    }
}
