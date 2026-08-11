namespace AssignmentSubmissionSystem.Application.AccountManagement;

public sealed class AccountManagementException : Exception
{
    public AccountManagementException(string message)
        : base(message)
    {
    }
}
