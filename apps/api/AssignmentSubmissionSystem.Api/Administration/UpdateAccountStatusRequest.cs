using System.ComponentModel.DataAnnotations;

namespace AssignmentSubmissionSystem.Api.Administration;

public sealed class UpdateAccountStatusRequest
{
    [Required]
    public bool? IsActive { get; init; }
}
