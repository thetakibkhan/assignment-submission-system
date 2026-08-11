using AssignmentSubmissionSystem.Domain.Academics;

namespace AssignmentSubmissionSystem.Application.AcademicSetup.TeacherResponsibilities;

public interface ITeacherResponsibilityService
{
    Task<TeacherResponsibility> CreateAsync(
        CreateTeacherResponsibilityCommand command,
        Guid assignedByUserId,
        CancellationToken cancellationToken);

    Task RevokeAsync(Guid id, Guid revokedByUserId, CancellationToken cancellationToken);
}
