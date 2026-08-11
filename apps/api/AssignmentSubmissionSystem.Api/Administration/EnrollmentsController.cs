using AssignmentSubmissionSystem.Api.Authentication;
using AssignmentSubmissionSystem.Application.AcademicSetup.Enrollments;
using AssignmentSubmissionSystem.Domain.Academics;
using AssignmentSubmissionSystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Administration;

[Authorize(Policy = AuthorizationPolicies.NormalAccess, Roles = RoleNames.Admin)]
[ApiController]
[Route("api/admin/enrollments")]
public sealed class EnrollmentsController : ControllerBase
{
    private readonly IStudentEnrollmentService _studentEnrollmentService;

    public EnrollmentsController(IStudentEnrollmentService studentEnrollmentService)
    {
        _studentEnrollmentService = studentEnrollmentService;
    }

    [HttpPost]
    public async Task<ActionResult<EnrollmentResponse>> CreateAsync(
        CreateStudentEnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            StudentEnrollment enrollment = await _studentEnrollmentService.CreateAsync(
                new CreateStudentEnrollmentCommand
                {
                    ClassCourseId = request.ClassCourseId,
                    StudentInstitutionalId = request.StudentInstitutionalId
                },
                User.GetRequiredUserId(),
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, EnrollmentResponse.From(enrollment));
        }
        catch (DuplicateActiveEnrollmentException exception)
        {
            return Conflict(CreateProblemDetails(exception.Message, "Active enrollment already exists"));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(CreateProblemDetails(exception.Message, "Student account is unavailable"));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(CreateProblemDetails(exception.Message, "Enrollment is not allowed"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:guid}/end")]
    public async Task<IActionResult> EndAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _studentEnrollmentService.EndAsync(id, User.GetRequiredUserId(), cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(CreateProblemDetails(exception.Message, "Enrollment is already ended"));
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
