namespace AssignmentSubmissionSystem.Api.Administration;

public sealed class CreatedManagedAccountResponse
{
    public ManagedAccountResponse Account { get; init; } = new();

    public string InstitutionalId { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public string TemporaryPassword { get; init; } = string.Empty;
}
