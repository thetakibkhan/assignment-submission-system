using AssignmentSubmissionSystem.Api.Authentication;
using AssignmentSubmissionSystem.Application.Submissions;
using AssignmentSubmissionSystem.Domain.Submissions;
using AssignmentSubmissionSystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Student;

[Authorize(Policy = AuthorizationPolicies.NormalAccess, Roles = RoleNames.Student)]
[ApiController]
[Route("api/student/assignments/{assignmentId:guid}/submission")]
public sealed class StudentSubmissionsController : ControllerBase
{
    private readonly ISubmissionService _submissionService;

    public StudentSubmissionsController(ISubmissionService submissionService)
    {
        _submissionService = submissionService;
    }

    [HttpGet]
    public async Task<ActionResult<StudentSubmissionResponse>> GetAsync(
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            Submission submission = await _submissionService.GetAsync(
                assignmentId,
                User.GetRequiredUserId(),
                cancellationToken);
            return Ok(StudentSubmissionResponse.From(submission));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public Task<ActionResult<StudentSubmissionResponse>> CreateAsync(Guid assignmentId, [FromForm] StudentSubmissionRequest request, CancellationToken cancellationToken)
    {
        return SaveAsync(() => _submissionService.CreateAsync(assignmentId, new CreateSubmissionCommand { TextAnswer = request.TextAnswer }, User.GetRequiredUserId(), cancellationToken), true);
    }

    [HttpPut]
    public Task<ActionResult<StudentSubmissionResponse>> UpdateAsync(Guid assignmentId, [FromForm] StudentSubmissionRequest request, CancellationToken cancellationToken)
    {
        return SaveAsync(() => _submissionService.UpdateAsync(assignmentId, new CreateSubmissionCommand { TextAnswer = request.TextAnswer }, User.GetRequiredUserId(), cancellationToken), false);
    }

    private async Task<ActionResult<StudentSubmissionResponse>> SaveAsync(Func<Task<Submission>> save, bool created)
    {
        try
        {
            StudentSubmissionResponse response = StudentSubmissionResponse.From(await save());
            return created ? StatusCode(StatusCodes.Status201Created, response) : Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message, Status = StatusCodes.Status400BadRequest, Title = "Submission is not valid" });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails { Detail = exception.Message, Status = StatusCodes.Status409Conflict, Title = "Submission action is not allowed" });
        }
    }
}
