using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Authentication;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("login")]
    public ActionResult Login(LoginRequest request)
    {
        return Unauthorized();
    }
}
