using AssignmentSubmissionSystem.Application.AcademicSetup.AcademicClasses;
using AssignmentSubmissionSystem.Api.Authentication;
using AssignmentSubmissionSystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Administration;

[Authorize(Policy = AuthorizationPolicies.NormalAccess, Roles = RoleNames.Admin)]
[ApiController]
[Route("api/admin/classes")]
public sealed class AcademicClassesController : ControllerBase
{
    private readonly IAcademicClassService _academicClassService;

    public AcademicClassesController(IAcademicClassService academicClassService)
    {
        _academicClassService = academicClassService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AcademicClassResponse>>> GetAllAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<AssignmentSubmissionSystem.Domain.Academics.AcademicClass> academicClasses = await _academicClassService.GetAllAsync(cancellationToken);

        return Ok(academicClasses.Select(ToResponse).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<AcademicClassResponse>> CreateAsync(
        CreateAcademicClassRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            AssignmentSubmissionSystem.Domain.Academics.AcademicClass academicClass = await _academicClassService.CreateAsync(
                new CreateAcademicClassCommand
                {
                    Code = request.Code,
                    Name = request.Name
                },
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, ToResponse(academicClass));
        }
        catch (DuplicateAcademicClassCodeException exception)
        {
            return Conflict(new ProblemDetails
            {
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict,
                Title = "Class code already exists"
            });
        }
    }


    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> ArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _academicClassService.ArchiveAsync(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AcademicClassResponse>> UpdateAsync(
        Guid id,
        CreateAcademicClassRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            AssignmentSubmissionSystem.Domain.Academics.AcademicClass academicClass = await _academicClassService.UpdateAsync(id, new CreateAcademicClassCommand { Code = request.Code, Name = request.Name }, cancellationToken);
            return Ok(ToResponse(academicClass));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (DuplicateAcademicClassCodeException exception)
        {
            return Conflict(new ProblemDetails { Detail = exception.Message, Status = StatusCodes.Status409Conflict, Title = "Class code already exists" });
        }
    }
    private static AcademicClassResponse ToResponse(
        AssignmentSubmissionSystem.Domain.Academics.AcademicClass academicClass)
    {
        return new AcademicClassResponse
        {
            Code = academicClass.Code,
            Id = academicClass.Id,
            IsArchived = academicClass.IsArchived,
            Name = academicClass.Name
        };
    }
}
