using System.Security.Claims;

namespace AssignmentSubmissionSystem.Api.Authentication;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetRequiredUserId(this ClaimsPrincipal principal)
    {
        string? userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out Guid parsedUserId))
        {
            throw new InvalidOperationException("The authenticated user identifier is invalid.");
        }

        return parsedUserId;
    }
}
