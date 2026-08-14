namespace AssignmentSubmissionSystem.Application.AcademicSetup.AcademicClasses;

public sealed class CreateAcademicClassCommand
{
    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}
