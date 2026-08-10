namespace AssignmentSubmissionSystem.Domain.Academics;

public sealed class TeacherResponsibility
{
    public TeacherResponsibility(
        Guid id,
        Guid teacherUserId,
        Guid classCourseId,
        Guid subjectId,
        Guid assignedByUserId,
        DateTimeOffset assignedAt)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(teacherUserId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(classCourseId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(subjectId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(assignedByUserId, Guid.Empty);

        Id = id;
        TeacherUserId = teacherUserId;
        ClassCourseId = classCourseId;
        SubjectId = subjectId;
        AssignedByUserId = assignedByUserId;
        AssignedAt = assignedAt;
    }

    public Guid Id { get; private set; }

    public Guid TeacherUserId { get; private set; }

    public Guid ClassCourseId { get; private set; }

    public Guid SubjectId { get; private set; }

    public Guid AssignedByUserId { get; private set; }

    public DateTimeOffset AssignedAt { get; private set; }

    public Guid? RevokedByUserId { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsActive => RevokedAt is null;

    public void Revoke(Guid revokedByUserId, DateTimeOffset revokedAt)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(revokedByUserId, Guid.Empty);

        if (!IsActive)
        {
            throw new InvalidOperationException("The teacher responsibility has already been revoked.");
        }

        if (revokedAt < AssignedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(revokedAt), "The revocation time cannot precede assignment.");
        }

        RevokedByUserId = revokedByUserId;
        RevokedAt = revokedAt;
    }
}
