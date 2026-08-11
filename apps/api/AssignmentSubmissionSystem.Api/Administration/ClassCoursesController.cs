using AssignmentSubmissionSystem.Application.AcademicSetup.ClassCourses;
using AssignmentSubmissionSystem.Api.Authentication;
using AssignmentSubmissionSystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Administration;

[Authorize(Policy = AuthorizationPolicies.NormalAccess, Roles = RoleNames.Admin)]
[ApiController]
[Route("api/admin/classes-courses")]
public sealed class ClassCoursesController : ControllerBase
{
    private readonly IClassCourseService _classCourseService;

    public ClassCoursesController(IClassCourseService classCourseService)
    {
        _classCourseService = classCourseService;
    }

    [HttpPost]
    public async Task<ActionResult<ClassCourseResponse>> CreateAsync(
        CreateClassCourseRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            AssignmentSubmissionSystem.Domain.Academics.ClassCourse classCourse = await _classCourseService.CreateAsync(
                new CreateClassCourseCommand
                {
                    Code = request.Code,
                    Name = request.Name
                },
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, ToResponse(classCourse));
        }
        catch (DuplicateClassCourseCodeException exception)
        {
            return Conflict(new ProblemDetails
            {
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict,
                Title = "Class/Course code already exists"
            });
        }
    }


    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> ArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _classCourseService.ArchiveAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClassCourseResponse>> UpdateAsync(
        Guid id,
        CreateClassCourseRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            AssignmentSubmissionSystem.Domain.Academics.ClassCourse classCourse = await _classCourseService.UpdateAsync(id, new CreateClassCourseCommand { Code = request.Code, Name = request.Name }, cancellationToken);
            return Ok(ToResponse(classCourse));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (DuplicateClassCourseCodeException exception)
        {
            return Conflict(new ProblemDetails { Detail = exception.Message, Status = StatusCodes.Status409Conflict, Title = "Class/Course code already exists" });
        }
    }
    private static ClassCourseResponse ToResponse(
        AssignmentSubmissionSystem.Domain.Academics.ClassCourse classCourse)
    {
        return new ClassCourseResponse
        {
            Code = classCourse.Code,
            Id = classCourse.Id,
            IsArchived = classCourse.IsArchived,
            Name = classCourse.Name
        };
    }
}
