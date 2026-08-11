namespace AssignmentSubmissionSystem.Application.AccountManagement;

public sealed class ResetManagedPassword
{
    public ManagedAccount Account { get; init; } = new();

    public string TemporaryPassword { get; init; } = string.Empty;
}
