using AssignmentSubmissionSystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Authentication;

[AllowAnonymous]
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthController(
        IJwtTokenService jwtTokenService,
        ILogger<AuthController> logger,
        UserManager<ApplicationUser> userManager)
    {
        _jwtTokenService = jwtTokenService;
        _logger = logger;
        _userManager = userManager;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> LoginAsync(
        LoginRequest request)
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

        string redirectPath = GetRedirectPath(role);
        string accessToken = _jwtTokenService.CreateAccessToken(user, role);

        Response.Cookies.Append("access_token", accessToken, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Strict,
            Secure = !HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(),
            MaxAge = TimeSpan.FromMinutes(15),
            Path = "/"
        });
        _logger.LogInformation("User {UserId} signed in with role {Role}.", user.Id, role);

        return Ok(new LoginResponse
        {
            RedirectPath = redirectPath,
            Role = role
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
