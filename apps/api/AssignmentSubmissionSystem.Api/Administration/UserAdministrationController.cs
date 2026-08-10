using AssignmentSubmissionSystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Administration;

[Authorize(Roles = RoleNames.Admin)]
[ApiController]
[Route("api/admin/users")]
public sealed class UserAdministrationController : ControllerBase
{
    private readonly ILogger<UserAdministrationController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserAdministrationController(
        ILogger<UserAdministrationController> logger,
        UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _userManager = userManager;
    }

    [HttpPut("{email}/activation")]
    public async Task<IActionResult> UpdateActivationAsync(
        string email,
        UpdateAccountStatusRequest request,
        CancellationToken cancellationToken)
    {
        ApplicationUser? user = await _userManager.FindByEmailAsync(email.Trim());

        if (user is null)
        {
            return NotFound();
        }

        bool isActive = request.IsActive ?? throw new InvalidOperationException("The account status is required.");
        user.IsActive = isActive;
        IdentityResult updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            return Problem(
                detail: "The account status could not be updated.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Account update failed");
        }

        _logger.LogInformation("User {UserId} account active status changed to {IsActive}.", user.Id, user.IsActive);

        return NoContent();
    }
}
