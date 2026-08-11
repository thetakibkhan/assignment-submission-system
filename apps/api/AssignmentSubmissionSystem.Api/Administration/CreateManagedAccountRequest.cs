using System.ComponentModel.DataAnnotations;

namespace AssignmentSubmissionSystem.Api.Administration;

public sealed class CreateManagedAccountRequest
{
    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; init; }

    [Required]
    [StringLength(200)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string InstitutionalId { get; init; } = string.Empty;

    [Required]
    [RegularExpression("^(Teacher|Student)$")]
    public string Role { get; init; } = string.Empty;
}
