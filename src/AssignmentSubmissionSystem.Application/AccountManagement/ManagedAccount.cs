namespace AssignmentSubmissionSystem.Application.AccountManagement;

public sealed class ManagedAccount
{
    public string? Email { get; init; }

    public string FullName { get; init; } = string.Empty;

    public Guid Id { get; init; }

    public string InstitutionalId { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public bool MustChangePassword { get; init; }

    public string Role { get; init; } = string.Empty;
}
