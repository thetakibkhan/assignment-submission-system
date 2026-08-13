using AssignmentSubmissionSystem.Domain.Submissions;

namespace AssignmentSubmissionSystem.Application.Submissions;

public sealed class StudentSubmissionDetails
{
    public bool ResultsAvailable { get; init; }

    public required Submission Submission { get; init; }
}
