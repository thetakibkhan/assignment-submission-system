namespace AssignmentSubmissionSystem.Infrastructure.Authentication;

public interface IJwtTokenService
{
    string CreateAccessToken(ApplicationUser user, string role);
}
