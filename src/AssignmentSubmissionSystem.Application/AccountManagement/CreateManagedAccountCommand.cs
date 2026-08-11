namespace AssignmentSubmissionSystem.Application.AccountManagement;

public sealed class CreateManagedAccountCommand
{
    public string? Email { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string InstitutionalId { get; init; } = string.Empty;

    public ManagedAccountRole Role { get; init; }
}
