namespace AssignmentSubmissionSystem.Infrastructure.Authentication;

public sealed class DemoAccountOptions
{
    public const string SectionName = "DemoAccounts";

    public string AdminInstitutionalId { get; init; } = "ADM-001";

    public string AdminPassword { get; init; } = string.Empty;

    public string StudentInstitutionalId { get; init; } = "STU-001";

    public string StudentPassword { get; init; } = string.Empty;

    public string TeacherInstitutionalId { get; init; } = "TCH-001";

    public string TeacherPassword { get; init; } = string.Empty;
}
