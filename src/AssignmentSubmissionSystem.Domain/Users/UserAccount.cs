namespace AssignmentSubmissionSystem.Domain.Users;

public sealed class UserAccount
{
    public UserAccount(
        Guid id,
        string fullName,
        string email,
        UserRole role)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentOutOfRangeException.ThrowIfNotEqual(true, Enum.IsDefined(role));

        Id = id;
        FullName = fullName.Trim();
        Email = email.Trim();
        Role = role;
        IsActive = true;
    }

    public Guid Id { get; }

    public string FullName { get; }

    public string Email { get; }

    public UserRole Role { get; }

    public bool IsActive { get; private set; }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Reactivate()
    {
        IsActive = true;
    }
}
