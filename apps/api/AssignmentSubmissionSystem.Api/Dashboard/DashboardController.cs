using AssignmentSubmissionSystem.Infrastructure.Authentication;
using AssignmentSubmissionSystem.Application.Dashboards;
using AssignmentSubmissionSystem.Api.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Dashboard;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardQuery _dashboardQuery;

    public DashboardController(IDashboardQuery dashboardQuery)
    {
        _dashboardQuery = dashboardQuery;
    }
    [Authorize(Policy = AuthorizationPolicies.NormalAccess, Roles = RoleNames.Admin)]
    [HttpGet("admin")]
    public async Task<ActionResult<AdminDashboardSummary>> GetAdminDashboard(CancellationToken cancellationToken)
    {
        return Ok(await _dashboardQuery.GetAdminAsync(cancellationToken));
    }

    [Authorize(Policy = AuthorizationPolicies.NormalAccess, Roles = RoleNames.Teacher)]
    [HttpGet("teacher")]
    public async Task<ActionResult<TeacherDashboardSummary>> GetTeacherDashboard(CancellationToken cancellationToken)
    {
        return Ok(await _dashboardQuery.GetTeacherAsync(User.GetRequiredUserId(), cancellationToken));
    }

    [Authorize(Policy = AuthorizationPolicies.NormalAccess, Roles = RoleNames.Student)]
    [HttpGet("student")]
    public async Task<ActionResult<StudentDashboardSummary>> GetStudentDashboard(CancellationToken cancellationToken)
    {
        return Ok(await _dashboardQuery.GetStudentAsync(User.GetRequiredUserId(), DateTimeOffset.UtcNow, cancellationToken));
    }
}
