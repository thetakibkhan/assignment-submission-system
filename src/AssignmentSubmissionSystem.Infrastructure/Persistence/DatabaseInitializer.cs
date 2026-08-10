using AssignmentSubmissionSystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AssignmentSubmissionSystem.Infrastructure.Persistence;

public sealed class DatabaseInitializer
{
    private readonly ApplicationDbContext _databaseContext;
    private readonly DemoAccountOptions _demoAccounts;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public DatabaseInitializer(
        ApplicationDbContext databaseContext,
        IOptions<DemoAccountOptions> demoAccounts,
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        _databaseContext = databaseContext;
        _demoAccounts = demoAccounts.Value;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_databaseContext.Database.IsRelational())
        {
            await _databaseContext.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await _databaseContext.Database.EnsureCreatedAsync(cancellationToken);
        }

        await EnsureRoleAsync(RoleNames.Admin);
        await EnsureRoleAsync(RoleNames.Teacher);
        await EnsureRoleAsync(RoleNames.Student);
        await EnsureUserAsync("Nusrat Jahan", "ADM-001", "admin@assignment.local", _demoAccounts.AdminPassword, RoleNames.Admin);
        await EnsureUserAsync("Rafiq Hasan", "TCH-001", "teacher@assignment.local", _demoAccounts.TeacherPassword, RoleNames.Teacher);
        await EnsureUserAsync("Ayesha Rahman", "STU-001", "student@assignment.local", _demoAccounts.StudentPassword, RoleNames.Student);
    }

    private async Task EnsureRoleAsync(string roleName)
    {
        if (await _roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        IdentityResult result = await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));

        if (!result.Succeeded)
        {
            throw new InvalidOperationException("The required application role could not be created.");
        }
    }

    private async Task EnsureUserAsync(
        string fullName,
        string institutionalId,
        string email,
        string password,
        string roleName)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Demo account passwords must be configured before the application starts.");
        }

        ApplicationUser? user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.CreateVersion7(),
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                IsActive = true,
                UserName = institutionalId
            };

            IdentityResult createResult = await _userManager.CreateAsync(user, password);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException("A required demo account could not be created.");
            }
        }

        if (!string.Equals(user.UserName, institutionalId, StringComparison.Ordinal))
        {
            user.UserName = institutionalId;
            IdentityResult updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException("A required demo account institutional ID could not be updated.");
            }
        }

        if (!await _userManager.IsInRoleAsync(user, roleName))
        {
            IdentityResult roleResult = await _userManager.AddToRoleAsync(user, roleName);

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException("A required demo account role could not be assigned.");
            }
        }
    }
}
