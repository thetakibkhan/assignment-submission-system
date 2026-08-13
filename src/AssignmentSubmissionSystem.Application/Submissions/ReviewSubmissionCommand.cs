namespace AssignmentSubmissionSystem.Application.Submissions;

public sealed class ReviewSubmissionCommand
{
    public string? Feedback { get; init; }

    public decimal? Marks { get; init; }
}
