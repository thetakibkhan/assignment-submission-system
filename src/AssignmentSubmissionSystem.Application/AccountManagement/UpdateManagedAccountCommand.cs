namespace AssignmentSubmissionSystem.Application.AccountManagement;

public sealed class UpdateManagedAccountCommand
{
    public string? Email { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string InstitutionalId { get; init; } = string.Empty;
}
