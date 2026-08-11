namespace AssignmentSubmissionSystem.Application.AccountManagement;

public interface IAccountManagementService
{
    Task ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken);

    Task<CreatedManagedAccount> CreateAsync(
        CreateManagedAccountCommand command,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ManagedAccount>> GetAllAsync(CancellationToken cancellationToken);

    Task<ResetManagedPassword> ResetPasswordAsync(
        string institutionalId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<ManagedAccount> SetActivationAsync(
        string institutionalId,
        bool isActive,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<ManagedAccount> UpdateAsync(
        string currentInstitutionalId,
        UpdateManagedAccountCommand command,
        Guid actorUserId,
        CancellationToken cancellationToken);
}
