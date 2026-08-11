using Microsoft.AspNetCore.Identity;

namespace AssignmentSubmissionSystem.Infrastructure.Authentication;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool MustChangePassword { get; set; }
}
