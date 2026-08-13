using AssignmentSubmissionSystem.Api.Authentication;
using AssignmentSubmissionSystem.Application.Submissions;
using AssignmentSubmissionSystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Teacher;

[Authorize(Policy = AuthorizationPolicies.NormalAccess, Roles = RoleNames.Teacher)]
[ApiController]
[Route("api/teacher/submissions")]
public sealed class TeacherSubmissionsController : ControllerBase
{
    private readonly ISubmissionService _submissionService;

    public TeacherSubmissionsController(ISubmissionService submissionService)
    {
        _submissionService = submissionService;
    }

    [HttpGet("/api/teacher/assignments/{assignmentId:guid}/submissions")]
    public async Task<ActionResult<IReadOnlyList<TeacherSubmissionResponse>>> GetQueueAsync(
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<TeacherSubmissionItem> items = await _submissionService.GetForTeacherAssignmentAsync(
                assignmentId,
                User.GetRequiredUserId(),
                cancellationToken);
            return Ok(items.Select(TeacherSubmissionResponse.From).ToList());
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{submissionId:guid}/start-review")]
    public Task<IActionResult> StartReviewAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        return ChangeAsync(() => _submissionService.StartReviewAsync(submissionId, User.GetRequiredUserId(), cancellationToken));
    }

    [HttpPut("{submissionId:guid}/review")]
    public Task<IActionResult> UpdateReviewAsync(
        Guid submissionId,
        ReviewSubmissionCommand command,
        CancellationToken cancellationToken)
    {
        return ChangeAsync(() => _submissionService.UpdateReviewAsync(submissionId, command, User.GetRequiredUserId(), cancellationToken));
    }

    [HttpPost("{submissionId:guid}/grade")]
    public Task<IActionResult> GradeAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        return ChangeAsync(() => _submissionService.GradeAsync(submissionId, User.GetRequiredUserId(), cancellationToken));
    }

    [HttpPost("{submissionId:guid}/reopen")]
    public Task<IActionResult> ReopenAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        return ChangeAsync(() => _submissionService.ReopenForCorrectionAsync(submissionId, User.GetRequiredUserId(), cancellationToken));
    }

    private async Task<IActionResult> ChangeAsync(Func<Task> action)
    {
        try
        {
            await action();
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message, Title = "Review is not valid" });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails { Detail = exception.Message, Title = "Review action is not allowed" });
        }
    }
}
