using AssignmentSubmissionSystem.Domain.Academics;

namespace AssignmentSubmissionSystem.Api.Administration;

public sealed class EnrollmentResponse
{
    public Guid AcademicClassId { get; init; }

    public DateTimeOffset EnrolledAt { get; init; }

    public Guid Id { get; init; }

    public bool IsActive { get; init; }

    public Guid StudentUserId { get; init; }

    public static EnrollmentResponse From(StudentEnrollment enrollment)
    {
        return new EnrollmentResponse
        {
            AcademicClassId = enrollment.AcademicClassId,
            EnrolledAt = enrollment.EnrolledAt,
            Id = enrollment.Id,
            IsActive = enrollment.IsActive,
            StudentUserId = enrollment.StudentUserId
        };
    }
}
