using AssignmentSubmissionSystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Dashboard;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    [Authorize(Roles = RoleNames.Admin)]
    [HttpGet("admin")]
    public ActionResult<object> GetAdminDashboard()
    {
        return Ok(new { message = "Admin dashboard access granted." });
    }

    [Authorize(Roles = RoleNames.Teacher)]
    [HttpGet("teacher")]
    public ActionResult<object> GetTeacherDashboard()
    {
        return Ok(new { message = "Teacher dashboard access granted." });
    }

    [Authorize(Roles = RoleNames.Student)]
    [HttpGet("student")]
    public ActionResult<object> GetStudentDashboard()
    {
        return Ok(new { message = "Student dashboard access granted." });
    }
}
