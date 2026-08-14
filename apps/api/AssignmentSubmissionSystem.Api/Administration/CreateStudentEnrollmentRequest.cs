using System.ComponentModel.DataAnnotations;

namespace AssignmentSubmissionSystem.Api.Administration;

public sealed class CreateStudentEnrollmentRequest
{
    public Guid AcademicClassId { get; init; }

    [Required]
    [StringLength(256)]
    public string StudentInstitutionalId { get; init; } = string.Empty;
}
