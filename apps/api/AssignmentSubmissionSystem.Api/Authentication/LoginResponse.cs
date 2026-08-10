namespace AssignmentSubmissionSystem.Api.Authentication;

public sealed class LoginResponse
{
    public string RedirectPath { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;
}
