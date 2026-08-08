using AssignmentSubmissionSystem.Domain.Users;

namespace AssignmentSubmissionSystem.Domain.UnitTests.Users;

public sealed class UserAccountTests
{
    [Fact]
    public void Deactivate_ShouldMarkAnActiveAccountAsInactive()
    {
        UserAccount userAccount = CreateStudentAccount();

        userAccount.Deactivate();

        Assert.False(userAccount.IsActive);
    }

    [Fact]
    public void Reactivate_ShouldMarkAnInactiveAccountAsActive()
    {
        UserAccount userAccount = CreateStudentAccount();
        userAccount.Deactivate();

        userAccount.Reactivate();

        Assert.True(userAccount.IsActive);
    }

    private static UserAccount CreateStudentAccount()
    {
        return new UserAccount(
            Guid.CreateVersion7(),
            "Ayesha Rahman",
            "ayesha.rahman@example.test",
            UserRole.Student);
    }
}
