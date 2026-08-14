namespace AssignmentSubmissionSystem.Application.Dashboards;

public sealed class AdminDashboardSummary
{
    public int ActiveAccounts { get; init; }
    public int ActiveAcademicClasses { get; init; }
    public int ActiveSubjects { get; init; }
    public int AssignmentCount { get; init; }
    public int SubmissionCount { get; init; }
}

public sealed class TeacherDashboardSummary
{
    public int DraftAssignments { get; init; }
    public int PublishedAssignments { get; init; }
    public int SubmissionsNeedingReview { get; init; }
    public int SubmissionsUnderReview { get; init; }
}

public sealed class StudentDashboardSummary
{
    public int GradedSubmissions { get; init; }
    public DateTimeOffset? NearestDeadline { get; init; }
    public int OpenAssignments { get; init; }
    public int SubmittedAssignments { get; init; }
}
