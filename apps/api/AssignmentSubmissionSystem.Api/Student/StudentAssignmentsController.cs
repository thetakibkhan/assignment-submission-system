using AssignmentSubmissionSystem.Api.Authentication;
using AssignmentSubmissionSystem.Application.Assignments;
using AssignmentSubmissionSystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Student;

[Authorize(Policy = AuthorizationPolicies.NormalAccess, Roles = RoleNames.Student)]
[ApiController]
[Route("api/student/assignments")]
public sealed class StudentAssignmentsController : ControllerBase
{
    private readonly IStudentAssignmentQuery _studentAssignmentQuery;

    public StudentAssignmentsController(IStudentAssignmentQuery studentAssignmentQuery)
    {
        _studentAssignmentQuery = studentAssignmentQuery;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StudentAssignmentItem>>> GetAllAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<StudentAssignmentItem> assignments = await _studentAssignmentQuery.GetAllAsync(
            User.GetRequiredUserId(),
            DateTimeOffset.UtcNow,
            cancellationToken);

        return Ok(assignments);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StudentAssignmentItem>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        StudentAssignmentItem? assignment = await _studentAssignmentQuery.GetByIdAsync(
            id,
            User.GetRequiredUserId(),
            DateTimeOffset.UtcNow,
            cancellationToken);

        return assignment is null ? NotFound() : Ok(assignment);
    }
}
