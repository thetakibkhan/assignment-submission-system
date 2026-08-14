using AssignmentSubmissionSystem.Application.Assignments;
using AssignmentSubmissionSystem.Domain.Academics;

namespace AssignmentSubmissionSystem.Application.AcademicSetup.TeacherResponsibilities;

public interface ITeacherResponsibilityRepository
{
    Task AddAsync(TeacherResponsibility responsibility, CancellationToken cancellationToken);

    Task<bool> ExistsActiveAsync(Guid academicClassId, Guid subjectId, CancellationToken cancellationToken);

    Task<bool> ExistsActiveForTeacherAsync(Guid academicClassId, Guid subjectId, Guid teacherUserId, CancellationToken cancellationToken);

    Task<TeacherResponsibility?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<TeacherAssignmentScope>> GetActiveScopesForTeacherAsync(Guid teacherUserId, CancellationToken cancellationToken);

    Task UpdateAsync(TeacherResponsibility responsibility, CancellationToken cancellationToken);
}
