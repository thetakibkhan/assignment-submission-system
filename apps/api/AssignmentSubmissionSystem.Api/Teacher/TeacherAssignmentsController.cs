using AssignmentSubmissionSystem.Api.Authentication;
using AssignmentSubmissionSystem.Application.Assignments;
using AssignmentSubmissionSystem.Domain.Assignments;
using AssignmentSubmissionSystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Teacher;

[Authorize(Policy = AuthorizationPolicies.NormalAccess, Roles = RoleNames.Teacher)]
[ApiController]
[Route("api/teacher/assignments")]
public sealed class TeacherAssignmentsController : ControllerBase
{
    private readonly IAssignmentService _assignmentService;
    public TeacherAssignmentsController(IAssignmentService assignmentService) => _assignmentService = assignmentService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AssignmentResponse>>> GetAllAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Assignment> assignments = await _assignmentService.GetForTeacherAsync(
            User.GetRequiredUserId(),
            cancellationToken);
        IReadOnlySet<Guid> assignmentIdsWithSubmissions = await _assignmentService
            .GetIdsWithSubmissionsAsync(assignments.Select(assignment => assignment.Id).ToArray(), cancellationToken);

        return Ok(assignments
            .Select(assignment => AssignmentResponse.From(
                assignment,
                !assignmentIdsWithSubmissions.Contains(assignment.Id)))
            .ToList());
    }
    [HttpGet("scopes")]
    public async Task<ActionResult<IReadOnlyList<TeacherAssignmentScope>>> GetScopesAsync(CancellationToken cancellationToken) => Ok(await _assignmentService.GetScopesForTeacherAsync(User.GetRequiredUserId(), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<AssignmentResponse>> CreateAsync(CreateAssignmentRequest request, CancellationToken cancellationToken)
    {
        try { Assignment assignment = await _assignmentService.CreateAsync(ToCommand(request), User.GetRequiredUserId(), cancellationToken); return StatusCode(StatusCodes.Status201Created, AssignmentResponse.From(assignment)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException exception) { return BadRequest(Problem(exception.Message)); }
    }
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AssignmentResponse>> UpdateAsync(Guid id, CreateAssignmentRequest request, CancellationToken cancellationToken)
    {
        try { Assignment assignment = await _assignmentService.UpdateAsync(id, ToCommand(request), User.GetRequiredUserId(), cancellationToken); return Ok(AssignmentResponse.From(assignment)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException exception) { return BadRequest(Problem(exception.Message)); }
        catch (InvalidOperationException exception) { return Conflict(Problem(exception.Message)); }
    }
    [HttpPost("{id:guid}/publish")]
    public Task<IActionResult> PublishAsync(Guid id, CancellationToken cancellationToken) => ChangeLifecycleAsync(() => _assignmentService.PublishAsync(id, User.GetRequiredUserId(), cancellationToken));
    [HttpPost("{id:guid}/unpublish")]
    public Task<IActionResult> UnpublishAsync(Guid id, CancellationToken cancellationToken) => ChangeLifecycleAsync(() => _assignmentService.UnpublishAsync(id, User.GetRequiredUserId(), cancellationToken));
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        try { await _assignmentService.DeleteAsync(id, User.GetRequiredUserId(), cancellationToken); return NoContent(); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException exception) { return Conflict(Problem(exception.Message)); }
    }
    private async Task<IActionResult> ChangeLifecycleAsync(Func<Task> action)
    {
        try { await action(); return NoContent(); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException exception) { return Conflict(Problem(exception.Message)); }
    }
    private static CreateAssignmentCommand ToCommand(CreateAssignmentRequest request) => new() { ClassCourseId = request.ClassCourseId, SubjectId = request.SubjectId, Title = request.Title, Description = request.Description, Deadline = request.Deadline, MaximumMarks = request.MaximumMarks, AllowSubmissionUpdates = request.AllowSubmissionUpdates };
    private static ProblemDetails Problem(string detail) => new() { Title = "Assignment action is not allowed", Detail = detail, Status = StatusCodes.Status400BadRequest };
}
