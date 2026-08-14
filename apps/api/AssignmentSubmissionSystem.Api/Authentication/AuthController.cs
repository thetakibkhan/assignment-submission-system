using AssignmentSubmissionSystem.Application.AccountManagement;
using AssignmentSubmissionSystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AssignmentSubmissionSystem.Api.Authentication;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAccountManagementService _accountManagementService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthController(
        IAccountManagementService accountManagementService,
        IJwtTokenService jwtTokenService,
        ILogger<AuthController> logger,
        UserManager<ApplicationUser> userManager)
    {
        _accountManagementService = accountManagementService;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
        _userManager = userManager;
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<LoginResponse>> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            Guid userId = User.GetRequiredUserId();
            await _accountManagementService.ChangePasswordAsync(
                userId,
                request.CurrentPassword,
                request.NewPassword,
                cancellationToken);
            ApplicationUser user = await _userManager.FindByIdAsync(userId.ToString())
                ?? throw new InvalidOperationException("The authenticated account was not found.");
            IList<string> roles = await _userManager.GetRolesAsync(user);
            string role = roles.SingleOrDefault()
                ?? throw new InvalidOperationException("The account does not have an assigned role.");
            string accessToken = _jwtTokenService.CreateAccessToken(user, role);

            AppendAccessTokenCookie(accessToken);
            _logger.LogInformation("User {UserId} changed their password.", user.Id);

            return Ok(new LoginResponse
            {
                RedirectPath = GetRedirectPath(role),
                RequiresPasswordChange = false,
                Role = role
            });
        }
        catch (AccountManagementException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest,
                Title = "Password change failed"
            });
        }
    }


    [AllowAnonymous]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("access_token", new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Strict,
            Secure = !HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(),
            Path = "/"
        });

        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("Authentication")]
    public async Task<ActionResult<LoginResponse>> LoginAsync(LoginRequest request)
    {
        ApplicationUser? user = await _userManager.FindByNameAsync(request.InstitutionalId.Trim());

        if (user is null || !user.IsActive || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new ProblemDetails
            {
                Detail = "The institutional ID or password is incorrect, or the account is inactive.",
                Status = StatusCodes.Status401Unauthorized,
                Title = "Sign-in failed"
            });
        }

        IList<string> roles = await _userManager.GetRolesAsync(user);
        string? role = roles.SingleOrDefault();

        if (role is null)
        {
            _logger.LogWarning("User {UserId} attempted to sign in without an assigned role.", user.Id);
            return Unauthorized(new ProblemDetails
            {
                Detail = "The account does not have an assigned role.",
                Status = StatusCodes.Status401Unauthorized,
                Title = "Sign-in failed"
            });
        }

        string accessToken = _jwtTokenService.CreateAccessToken(user, role);

        AppendAccessTokenCookie(accessToken);
        _logger.LogInformation("User {UserId} signed in with role {Role}.", user.Id, role);

        return Ok(new LoginResponse
        {
            RedirectPath = user.MustChangePassword ? "/change-password" : GetRedirectPath(role),
            RequiresPasswordChange = user.MustChangePassword,
            Role = role
        });
    }

    private void AppendAccessTokenCookie(string accessToken)
    {
        Response.Cookies.Append("access_token", accessToken, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Strict,
            Secure = !HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(),
            MaxAge = TimeSpan.FromMinutes(15),
            Path = "/"
        });
    }

    private static string GetRedirectPath(string role)
    {
        return role switch
        {
            RoleNames.Admin => "/admin",
            RoleNames.Teacher => "/teacher",
            RoleNames.Student => "/student",
            _ => throw new InvalidOperationException("The user has an unsupported role.")
        };
    }
}
