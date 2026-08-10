namespace AssignmentSubmissionSystem.Application.AcademicSetup.Subjects;

public sealed class CreateSubjectCommand
{
    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}
