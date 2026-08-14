using AssignmentSubmissionSystem.Application.AcademicSetup.TeacherResponsibilities;
using AssignmentSubmissionSystem.Application.Assignments;
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

    public Task<bool> ExistsActiveAsync(Guid academicClassId, Guid subjectId, CancellationToken cancellationToken)
    {
        return _databaseContext.TeacherResponsibilities.AnyAsync(
            responsibility => responsibility.AcademicClassId == academicClassId
                && responsibility.SubjectId == subjectId
                && responsibility.RevokedAt == null,
            cancellationToken);
    }

    public Task<bool> ExistsActiveForTeacherAsync(Guid academicClassId, Guid subjectId, Guid teacherUserId, CancellationToken cancellationToken)
    {
        return _databaseContext.TeacherResponsibilities.AnyAsync(
            responsibility => responsibility.AcademicClassId == academicClassId
                && responsibility.SubjectId == subjectId
                && responsibility.TeacherUserId == teacherUserId
                && responsibility.RevokedAt == null,
            cancellationToken);
    }

    public Task<TeacherResponsibility?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _databaseContext.TeacherResponsibilities.SingleOrDefaultAsync(
            responsibility => responsibility.Id == id,
            cancellationToken);
    }

    public async Task<IReadOnlyList<TeacherAssignmentScope>> GetActiveScopesForTeacherAsync(Guid teacherUserId, CancellationToken cancellationToken)
    {
        return await (from responsibility in _databaseContext.TeacherResponsibilities.AsNoTracking()
                      join academicClass in _databaseContext.AcademicClasses.AsNoTracking() on responsibility.AcademicClassId equals academicClass.Id
                      join subject in _databaseContext.Subjects.AsNoTracking() on responsibility.SubjectId equals subject.Id
                      where responsibility.TeacherUserId == teacherUserId
                          && responsibility.RevokedAt == null
                          && !academicClass.IsArchived
                          && !subject.IsArchived
                      orderby academicClass.Name, subject.Name
                      select new TeacherAssignmentScope
                      {
                          AcademicClassId = academicClass.Id,
                          AcademicClassName = academicClass.Name,
                          SubjectId = subject.Id,
                          SubjectName = subject.Name
                      }).ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(TeacherResponsibility responsibility, CancellationToken cancellationToken)
    {
        _databaseContext.TeacherResponsibilities.Update(responsibility);
        await _databaseContext.SaveChangesAsync(cancellationToken);
    }
}
