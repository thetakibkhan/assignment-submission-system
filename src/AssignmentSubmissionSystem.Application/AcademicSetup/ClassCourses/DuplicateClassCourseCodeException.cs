namespace AssignmentSubmissionSystem.Application.AcademicSetup.ClassCourses;

public sealed class DuplicateClassCourseCodeException : Exception
{
    public DuplicateClassCourseCodeException(string code)
        : base($"A Class/Course with code {code} already exists.")
    {
    }
}
