using AssignmentSubmissionSystem.Domain.Academics;

namespace AssignmentSubmissionSystem.Api.Administration;

public sealed class TeacherResponsibilityResponse
{
    public DateTimeOffset AssignedAt { get; init; }

    public Guid ClassCourseId { get; init; }

    public Guid Id { get; init; }

    public bool IsActive { get; init; }

    public Guid SubjectId { get; init; }

    public Guid TeacherUserId { get; init; }

    public static TeacherResponsibilityResponse From(TeacherResponsibility responsibility)
    {
        return new TeacherResponsibilityResponse
        {
            AssignedAt = responsibility.AssignedAt,
            ClassCourseId = responsibility.ClassCourseId,
            Id = responsibility.Id,
            IsActive = responsibility.IsActive,
            SubjectId = responsibility.SubjectId,
            TeacherUserId = responsibility.TeacherUserId
        };
    }
}
