namespace AssignmentSubmissionSystem.Infrastructure.Submissions;

public sealed class SubmissionStorageOptions
{
    public const string SectionName = "SubmissionStorage";

    public string RootPath { get; init; } = string.Empty;
}
