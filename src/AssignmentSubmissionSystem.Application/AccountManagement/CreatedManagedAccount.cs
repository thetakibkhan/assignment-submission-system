namespace AssignmentSubmissionSystem.Application.AccountManagement;

public sealed class CreatedManagedAccount
{
    public ManagedAccount Account { get; init; } = new();

    public string TemporaryPassword { get; init; } = string.Empty;
}
