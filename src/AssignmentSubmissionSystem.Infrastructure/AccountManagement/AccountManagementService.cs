using System.Security.Cryptography;
using AssignmentSubmissionSystem.Application.AccountManagement;
using AssignmentSubmissionSystem.Domain.Accounts;
using AssignmentSubmissionSystem.Infrastructure.Authentication;
using AssignmentSubmissionSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Infrastructure.AccountManagement;

public sealed class AccountManagementService : IAccountManagementService
{
    private const int TemporaryPasswordLength = 20;

    private readonly ApplicationDbContext _databaseContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountManagementService(
        ApplicationDbContext databaseContext,
        UserManager<ApplicationUser> userManager)
    {
        _databaseContext = databaseContext;
        _userManager = userManager;
    }

    public async Task ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ApplicationUser user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException("The authenticated account was not found.");
        IdentityResult changeResult = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        if (!changeResult.Succeeded)
        {
            throw new AccountManagementException("The password could not be changed.");
        }

        user.MustChangePassword = false;
        IdentityResult updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            throw new AccountManagementException("The password could not be changed.");
        }
    }

    public async Task<CreatedManagedAccount> CreateAsync(
        CreateManagedAccountCommand command,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        string fullName = NormalizeRequired(command.FullName, nameof(command.FullName), 200);
        string institutionalId = NormalizeRequired(command.InstitutionalId, nameof(command.InstitutionalId), 256);
        string? email = NormalizeOptional(command.Email, 256);
        string roleName = GetRoleName(command.Role);
        string temporaryPassword = GenerateTemporaryPassword();
        ApplicationUser user = new()
        {
            Email = email,
            FullName = fullName,
            Id = Guid.CreateVersion7(),
            IsActive = true,
            MustChangePassword = true,
            UserName = institutionalId
        };

        await using var transaction = await _databaseContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            IdentityResult createResult = await _userManager.CreateAsync(user, temporaryPassword);

            if (!createResult.Succeeded)
            {
                if (createResult.Errors.Any(error => error.Code == "DuplicateUserName"))
                {
                    throw new AccountManagementException("The institutional ID is already in use.");
                }

                throw new AccountManagementException("The account could not be created. Check that the institutional ID is valid.");
            }

            IdentityResult roleResult = await _userManager.AddToRoleAsync(user, roleName);

            if (!roleResult.Succeeded)
            {
                throw new AccountManagementException("The account role could not be assigned.");
            }

            await RecordAuditEventAsync(
                actorUserId,
                user.Id,
                AccountAuditEventType.AccountCreated,
                $"Created {roleName} account.",
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return new CreatedManagedAccount
        {
            Account = await ToManagedAccountAsync(user),
            TemporaryPassword = temporaryPassword
        };
    }

    public async Task<IReadOnlyList<ManagedAccount>> GetAllAsync(CancellationToken cancellationToken)
    {
        List<ApplicationUser> users = await _databaseContext.Users
            .AsNoTracking()
            .OrderBy(user => user.UserName)
            .ToListAsync(cancellationToken);
        var accounts = new List<ManagedAccount>(users.Count);

        foreach (ApplicationUser user in users)
        {
            accounts.Add(await ToManagedAccountAsync(user));
        }

        return accounts;
    }

    public async Task<ResetManagedPassword> ResetPasswordAsync(
        string institutionalId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        ApplicationUser user = await GetRequiredUserAsync(institutionalId, cancellationToken);
        string temporaryPassword = GenerateTemporaryPassword();
        string resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        await using var transaction = await _databaseContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            IdentityResult resetResult = await _userManager.ResetPasswordAsync(user, resetToken, temporaryPassword);

            if (!resetResult.Succeeded)
            {
                throw new AccountManagementException("The password could not be reset.");
            }

            user.MustChangePassword = true;
            IdentityResult updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                throw new AccountManagementException("The password could not be reset.");
            }

            await RecordAuditEventAsync(
                actorUserId,
                user.Id,
                AccountAuditEventType.PasswordReset,
                "Reset password and required a password change.",
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return new ResetManagedPassword
        {
            Account = await ToManagedAccountAsync(user),
            TemporaryPassword = temporaryPassword
        };
    }

    public async Task<ManagedAccount> SetActivationAsync(
        string institutionalId,
        bool isActive,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        ApplicationUser user = await GetRequiredUserAsync(institutionalId, cancellationToken);

        if (user.IsActive == isActive)
        {
            return await ToManagedAccountAsync(user);
        }

        user.IsActive = isActive;
        IdentityResult updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            throw new AccountManagementException("The account status could not be updated.");
        }

        await RecordAuditEventAsync(
            actorUserId,
            user.Id,
            AccountAuditEventType.ActivationChanged,
            isActive ? "Activated account." : "Deactivated account.",
            cancellationToken);

        return await ToManagedAccountAsync(user);
    }

    public async Task<ManagedAccount> UpdateAsync(
        string currentInstitutionalId,
        UpdateManagedAccountCommand command,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        ApplicationUser user = await GetRequiredUserAsync(currentInstitutionalId, cancellationToken);
        string fullName = NormalizeRequired(command.FullName, nameof(command.FullName), 200);
        string institutionalId = NormalizeRequired(command.InstitutionalId, nameof(command.InstitutionalId), 256);
        string? email = NormalizeOptional(command.Email, 256);
        var changedFields = new List<string>();

        if (!string.Equals(user.FullName, fullName, StringComparison.Ordinal))
        {
            user.FullName = fullName;
            changedFields.Add("full name");
        }

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            user.Email = email;
            changedFields.Add("email");
        }

        if (!string.Equals(user.UserName, institutionalId, StringComparison.Ordinal))
        {
            user.UserName = institutionalId;
            changedFields.Add("institutional ID");
        }

        if (changedFields.Count == 0)
        {
            return await ToManagedAccountAsync(user);
        }

        IdentityResult updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            if (updateResult.Errors.Any(error => error.Code == "DuplicateUserName"))
            {
                throw new AccountManagementException("The institutional ID is already in use.");
            }

            throw new AccountManagementException("The account profile could not be updated. Check that the account details are valid.");
        }

        await RecordAuditEventAsync(
            actorUserId,
            user.Id,
            AccountAuditEventType.ProfileUpdated,
            "Updated " + string.Join(", ", changedFields) + ".",
            cancellationToken);

        return await ToManagedAccountAsync(user);
    }

    private static string GenerateTemporaryPassword()
    {
        const string lowercaseLetters = "abcdefghjkmnpqrstuvwxyz";
        const string uppercaseLetters = "ABCDEFGHJKMNPQRSTUVWXYZ";
        const string digits = "23456789";
        const string symbols = "!@#$%";
        const string allCharacters = lowercaseLetters + uppercaseLetters + digits + symbols;
        char[] password = new char[TemporaryPasswordLength];

        password[0] = lowercaseLetters[RandomNumberGenerator.GetInt32(lowercaseLetters.Length)];
        password[1] = uppercaseLetters[RandomNumberGenerator.GetInt32(uppercaseLetters.Length)];
        password[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        password[3] = symbols[RandomNumberGenerator.GetInt32(symbols.Length)];

        for (int index = 4; index < password.Length; index++)
        {
            password[index] = allCharacters[RandomNumberGenerator.GetInt32(allCharacters.Length)];
        }

        for (int index = password.Length - 1; index > 0; index--)
        {
            int replacementIndex = RandomNumberGenerator.GetInt32(index + 1);
            (password[index], password[replacementIndex]) = (password[replacementIndex], password[index]);
        }

        return new string(password);
    }

    private async Task<ApplicationUser> GetRequiredUserAsync(
        string institutionalId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ApplicationUser? user = await _userManager.FindByNameAsync(institutionalId.Trim());

        return user ?? throw new KeyNotFoundException("The requested account was not found.");
    }

    private static string GetRoleName(ManagedAccountRole role)
    {
        return role switch
        {
            ManagedAccountRole.Student => RoleNames.Student,
            ManagedAccountRole.Teacher => RoleNames.Teacher,
            _ => throw new ArgumentOutOfRangeException(nameof(role), "Only Teacher and Student accounts can be created.")
        };
    }

    private static string NormalizeRequired(string value, string parameterName, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        string normalizedValue = value.Trim();

        if (normalizedValue.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(parameterName, $"The value cannot exceed {maxLength} characters.");
        }

        return normalizedValue;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalizedValue = value.Trim();

        if (normalizedValue.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"The value cannot exceed {maxLength} characters.");
        }

        return normalizedValue;
    }

    private async Task RecordAuditEventAsync(
        Guid actorUserId,
        Guid targetUserId,
        AccountAuditEventType eventType,
        string changeSummary,
        CancellationToken cancellationToken)
    {
        AccountAuditEvent auditEvent = new(
            Guid.CreateVersion7(),
            actorUserId,
            targetUserId,
            eventType,
            changeSummary,
            DateTimeOffset.UtcNow);
        await _databaseContext.AccountAuditEvents.AddAsync(auditEvent, cancellationToken);
        await _databaseContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ManagedAccount> ToManagedAccountAsync(ApplicationUser user)
    {
        IList<string> roles = await _userManager.GetRolesAsync(user);
        string role = roles.SingleOrDefault()
            ?? throw new InvalidOperationException("The account does not have an assigned role.");

        return new ManagedAccount
        {
            Email = user.Email,
            FullName = user.FullName,
            Id = user.Id,
            InstitutionalId = user.UserName ?? string.Empty,
            IsActive = user.IsActive,
            MustChangePassword = user.MustChangePassword,
            Role = role
        };
    }
}
