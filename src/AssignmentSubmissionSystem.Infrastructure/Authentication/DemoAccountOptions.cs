namespace AssignmentSubmissionSystem.Infrastructure.Authentication;

public sealed class DemoAccountOptions
{
    public const string SectionName = "DemoAccounts";

    public string AdminPassword { get; init; } = string.Empty;

    public string StudentPassword { get; init; } = string.Empty;

    public string TeacherPassword { get; init; } = string.Empty;
}
