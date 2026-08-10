namespace AssignmentSubmissionSystem.Domain.Academics;

public sealed class StudentEnrollment
{
    public StudentEnrollment(
        Guid id,
        Guid studentUserId,
        Guid classCourseId,
        Guid enrolledByUserId,
        DateTimeOffset enrolledAt)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(studentUserId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(classCourseId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(enrolledByUserId, Guid.Empty);

        Id = id;
        StudentUserId = studentUserId;
        ClassCourseId = classCourseId;
        EnrolledByUserId = enrolledByUserId;
        EnrolledAt = enrolledAt;
    }

    public Guid Id { get; }

    public Guid StudentUserId { get; }

    public Guid ClassCourseId { get; }

    public Guid EnrolledByUserId { get; }

    public DateTimeOffset EnrolledAt { get; }

    public Guid? EndedByUserId { get; private set; }

    public DateTimeOffset? EndedAt { get; private set; }

    public bool IsActive => EndedAt is null;

    public void End(Guid endedByUserId, DateTimeOffset endedAt)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(endedByUserId, Guid.Empty);

        if (!IsActive)
        {
            throw new InvalidOperationException("The enrollment has already ended.");
        }

        if (endedAt < EnrolledAt)
        {
            throw new ArgumentOutOfRangeException(nameof(endedAt), "The end time cannot precede enrollment.");
        }

        EndedByUserId = endedByUserId;
        EndedAt = endedAt;
    }
}
