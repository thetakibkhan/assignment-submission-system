namespace AssignmentSubmissionSystem.Infrastructure.Authentication;

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";

    public bool Enabled { get; init; }

    public string Email { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string InstitutionalId { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
