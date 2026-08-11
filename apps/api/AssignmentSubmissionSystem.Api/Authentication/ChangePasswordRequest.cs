using System.ComponentModel.DataAnnotations;

namespace AssignmentSubmissionSystem.Api.Authentication;

public sealed class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; init; } = string.Empty;
}
