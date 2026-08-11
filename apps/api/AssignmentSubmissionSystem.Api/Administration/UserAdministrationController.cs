using AssignmentSubmissionSystem.Api.Authentication;
using AssignmentSubmissionSystem.Application.AccountManagement;
using AssignmentSubmissionSystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Administration;

[Authorize(Policy = AuthorizationPolicies.NormalAccess, Roles = RoleNames.Admin)]
[ApiController]
[Route("api/admin/users")]
public sealed class UserAdministrationController : ControllerBase
{
    private readonly IAccountManagementService _accountManagementService;
    private readonly ILogger<UserAdministrationController> _logger;

    public UserAdministrationController(
        IAccountManagementService accountManagementService,
        ILogger<UserAdministrationController> logger)
    {
        _accountManagementService = accountManagementService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<CreatedManagedAccountResponse>> CreateAsync(
        CreateManagedAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse(request.Role, ignoreCase: false, out ManagedAccountRole role))
        {
            return BadRequest(CreateProblemDetails("The account role is invalid.", "Account creation failed"));
        }

        try
        {
            CreatedManagedAccount createdAccount = await _accountManagementService.CreateAsync(
                new CreateManagedAccountCommand
                {
                    Email = request.Email,
                    FullName = request.FullName,
                    InstitutionalId = request.InstitutionalId,
                    Role = role
                },
                User.GetRequiredUserId(),
                cancellationToken);

            _logger.LogInformation(
                "Admin {ActorUserId} created account {TargetUserId} with role {Role}.",
                User.GetRequiredUserId(),
                createdAccount.Account.Id,
                createdAccount.Account.Role);

            return StatusCode(StatusCodes.Status201Created, new CreatedManagedAccountResponse
            {
                Account = ManagedAccountResponse.From(createdAccount.Account),
                InstitutionalId = createdAccount.Account.InstitutionalId,
                Role = createdAccount.Account.Role,
                TemporaryPassword = createdAccount.TemporaryPassword
            });
        }
        catch (AccountManagementException exception)
        {
            return BadRequest(CreateProblemDetails(exception.Message, "Account creation failed"));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(CreateProblemDetails(exception.Message, "Account creation failed"));
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ManagedAccountResponse>>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ManagedAccount> accounts = await _accountManagementService.GetAllAsync(cancellationToken);

        return Ok(accounts.Select(ManagedAccountResponse.From).ToList());
    }

    [HttpPost("{institutionalId}/reset-password")]
    public async Task<ActionResult<ResetPasswordResponse>> ResetPasswordAsync(
        string institutionalId,
        CancellationToken cancellationToken)
    {
        try
        {
            ResetManagedPassword resetPassword = await _accountManagementService.ResetPasswordAsync(
                institutionalId,
                User.GetRequiredUserId(),
                cancellationToken);

            _logger.LogInformation(
                "Admin {ActorUserId} reset password for account {TargetUserId}.",
                User.GetRequiredUserId(),
                resetPassword.Account.Id);

            return Ok(new ResetPasswordResponse
            {
                TemporaryPassword = resetPassword.TemporaryPassword
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (AccountManagementException exception)
        {
            return BadRequest(CreateProblemDetails(exception.Message, "Password reset failed"));
        }
    }

    [HttpPut("{institutionalId}/activation")]
    public async Task<IActionResult> UpdateActivationAsync(
        string institutionalId,
        UpdateAccountStatusRequest request,
        CancellationToken cancellationToken)
    {
        bool isActive = request.IsActive ?? throw new InvalidOperationException("The account status is required.");

        try
        {
            ManagedAccount account = await _accountManagementService.SetActivationAsync(
                institutionalId,
                isActive,
                User.GetRequiredUserId(),
                cancellationToken);

            _logger.LogInformation(
                "Admin {ActorUserId} set account {TargetUserId} active status to {IsActive}.",
                User.GetRequiredUserId(),
                account.Id,
                account.IsActive);

            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (AccountManagementException exception)
        {
            return BadRequest(CreateProblemDetails(exception.Message, "Account update failed"));
        }
    }

    [HttpPut("{institutionalId}")]
    public async Task<ActionResult<ManagedAccountResponse>> UpdateAsync(
        string institutionalId,
        UpdateManagedAccountRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            ManagedAccount account = await _accountManagementService.UpdateAsync(
                institutionalId,
                new UpdateManagedAccountCommand
                {
                    Email = request.Email,
                    FullName = request.FullName,
                    InstitutionalId = request.InstitutionalId
                },
                User.GetRequiredUserId(),
                cancellationToken);

            _logger.LogInformation(
                "Admin {ActorUserId} updated account {TargetUserId}.",
                User.GetRequiredUserId(),
                account.Id);

            return Ok(ManagedAccountResponse.From(account));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (AccountManagementException exception)
        {
            return BadRequest(CreateProblemDetails(exception.Message, "Account update failed"));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(CreateProblemDetails(exception.Message, "Account update failed"));
        }
    }

    private static ProblemDetails CreateProblemDetails(string detail, string title)
    {
        return new ProblemDetails
        {
            Detail = detail,
            Status = StatusCodes.Status400BadRequest,
            Title = title
        };
    }
}
