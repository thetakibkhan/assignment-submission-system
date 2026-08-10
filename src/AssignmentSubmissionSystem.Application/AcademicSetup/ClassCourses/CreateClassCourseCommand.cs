namespace AssignmentSubmissionSystem.Application.AcademicSetup.ClassCourses;

public sealed class CreateClassCourseCommand
{
    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}
