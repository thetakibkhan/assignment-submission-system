namespace AssignmentSubmissionSystem.Application.AcademicSetup.AcademicClasses;

public sealed class DuplicateAcademicClassCodeException : Exception
{
    public DuplicateAcademicClassCodeException(string code)
        : base($"A Class with code {code} already exists.")
    {
    }
}
