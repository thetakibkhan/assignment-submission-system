namespace AssignmentSubmissionSystem.Api.Administration;

public sealed class ClassCourseResponse
{
    public string Code { get; init; } = string.Empty;

    public Guid Id { get; init; }

    public bool IsArchived { get; init; }

    public string Name { get; init; } = string.Empty;
}
