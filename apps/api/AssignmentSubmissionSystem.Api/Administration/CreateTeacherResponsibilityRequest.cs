using System.ComponentModel.DataAnnotations;

namespace AssignmentSubmissionSystem.Api.Administration;

public sealed class CreateTeacherResponsibilityRequest
{
    public Guid AcademicClassId { get; init; }

    public Guid SubjectId { get; init; }

    [Required]
    [StringLength(256)]
    public string TeacherInstitutionalId { get; init; } = string.Empty;
}
