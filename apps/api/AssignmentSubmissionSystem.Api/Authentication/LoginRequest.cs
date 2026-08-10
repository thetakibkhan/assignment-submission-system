using System.ComponentModel.DataAnnotations;

namespace AssignmentSubmissionSystem.Api.Authentication;

public sealed class LoginRequest
{
    [Required]
    [StringLength(64)]
    public string InstitutionalId { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}
