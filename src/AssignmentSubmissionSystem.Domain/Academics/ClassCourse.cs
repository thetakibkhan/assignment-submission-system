namespace AssignmentSubmissionSystem.Domain.Academics;

public sealed class ClassCourse
{
    public ClassCourse(Guid id, string name, string code)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);

        Id = id;
        Name = NormalizeRequiredText(name, nameof(name));
        Code = NormalizeRequiredText(code, nameof(code));
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string Code { get; private set; }

    public bool IsArchived { get; private set; }

    public void Update(string name, string code)
    {
        Name = NormalizeRequiredText(name, nameof(name));
        Code = NormalizeRequiredText(code, nameof(code));
    }

    public void Archive()
    {
        IsArchived = true;
    }

    private static string NormalizeRequiredText(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }
}
