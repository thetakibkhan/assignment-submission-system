using AssignmentSubmissionSystem.Api.Authentication;
using AssignmentSubmissionSystem.Application.AcademicSetup.TeacherResponsibilities;
using AssignmentSubmissionSystem.Domain.Academics;
using AssignmentSubmissionSystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Administration;

[Authorize(Policy = AuthorizationPolicies.NormalAccess, Roles = RoleNames.Admin)]
[ApiController]
[Route("api/admin/teacher-responsibilities")]
public sealed class TeacherResponsibilitiesController : ControllerBase
{
    private readonly ITeacherResponsibilityService _teacherResponsibilityService;

    public TeacherResponsibilitiesController(ITeacherResponsibilityService teacherResponsibilityService)
    {
        _teacherResponsibilityService = teacherResponsibilityService;
    }

    [HttpPost]
    public async Task<ActionResult<TeacherResponsibilityResponse>> CreateAsync(
        CreateTeacherResponsibilityRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            TeacherResponsibility responsibility = await _teacherResponsibilityService.CreateAsync(
                new CreateTeacherResponsibilityCommand
                {
                    ClassCourseId = request.ClassCourseId,
                    SubjectId = request.SubjectId,
                    TeacherInstitutionalId = request.TeacherInstitutionalId
                },
                User.GetRequiredUserId(),
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, TeacherResponsibilityResponse.From(responsibility));
        }
        catch (DuplicateActiveTeacherResponsibilityException exception)
        {
            return Conflict(CreateProblemDetails(exception.Message, "Active Teacher responsibility already exists"));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(CreateProblemDetails(exception.Message, "Teacher account is unavailable"));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(CreateProblemDetails(exception.Message, "Teacher responsibility is not allowed"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/revoke")]
    public async Task<IActionResult> RevokeAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _teacherResponsibilityService.RevokeAsync(id, User.GetRequiredUserId(), cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(CreateProblemDetails(exception.Message, "Teacher responsibility is already revoked"));
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
