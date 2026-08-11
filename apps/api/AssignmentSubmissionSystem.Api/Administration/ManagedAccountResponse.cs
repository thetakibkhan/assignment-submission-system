using AssignmentSubmissionSystem.Application.AccountManagement;

namespace AssignmentSubmissionSystem.Api.Administration;

public sealed class ManagedAccountResponse
{
    public string? Email { get; init; }

    public string FullName { get; init; } = string.Empty;

    public Guid Id { get; init; }

    public string InstitutionalId { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public bool MustChangePassword { get; init; }

    public string Role { get; init; } = string.Empty;

    public static ManagedAccountResponse From(ManagedAccount account)
    {
        return new ManagedAccountResponse
        {
            Email = account.Email,
            FullName = account.FullName,
            Id = account.Id,
            InstitutionalId = account.InstitutionalId,
            IsActive = account.IsActive,
            MustChangePassword = account.MustChangePassword,
            Role = account.Role
        };
    }
}
