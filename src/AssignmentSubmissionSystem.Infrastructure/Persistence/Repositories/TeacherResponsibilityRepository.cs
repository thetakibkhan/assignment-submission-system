using AssignmentSubmissionSystem.Application.AcademicSetup.TeacherResponsibilities;
using AssignmentSubmissionSystem.Domain.Academics;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Infrastructure.Persistence.Repositories;

public sealed class TeacherResponsibilityRepository : ITeacherResponsibilityRepository
{
    private readonly ApplicationDbContext _databaseContext;

    public TeacherResponsibilityRepository(ApplicationDbContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public async Task AddAsync(TeacherResponsibility responsibility, CancellationToken cancellationToken)
    {
        await _databaseContext.TeacherResponsibilities.AddAsync(responsibility, cancellationToken);
        await _databaseContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExistsActiveAsync(Guid classCourseId, Guid subjectId, CancellationToken cancellationToken)
    {
        return _databaseContext.TeacherResponsibilities.AnyAsync(
            responsibility => responsibility.ClassCourseId == classCourseId
                && responsibility.SubjectId == subjectId
                && responsibility.RevokedAt == null,
            cancellationToken);
    }

    public Task<TeacherResponsibility?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _databaseContext.TeacherResponsibilities.SingleOrDefaultAsync(
            responsibility => responsibility.Id == id,
            cancellationToken);
    }

    public async Task UpdateAsync(TeacherResponsibility responsibility, CancellationToken cancellationToken)
    {
        _databaseContext.TeacherResponsibilities.Update(responsibility);
        await _databaseContext.SaveChangesAsync(cancellationToken);
    }
}
