using AssignmentSubmissionSystem.Application.AcademicSetup;
using AssignmentSubmissionSystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;

namespace AssignmentSubmissionSystem.Infrastructure.AcademicSetup;

public sealed class AcademicUserDirectory : IAcademicUserDirectory
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AcademicUserDirectory(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public Task<Guid?> GetActiveStudentIdAsync(string institutionalId, CancellationToken cancellationToken)
    {
        return GetActiveUserIdAsync(institutionalId, RoleNames.Student, cancellationToken);
    }

    public Task<Guid?> GetActiveTeacherIdAsync(string institutionalId, CancellationToken cancellationToken)
    {
        return GetActiveUserIdAsync(institutionalId, RoleNames.Teacher, cancellationToken);
    }

    private async Task<Guid?> GetActiveUserIdAsync(
        string institutionalId,
        string roleName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ApplicationUser? user = await _userManager.FindByNameAsync(institutionalId.Trim());

        if (user is null || !user.IsActive || !await _userManager.IsInRoleAsync(user, roleName))
        {
            return null;
        }

        return user.Id;
    }
}
