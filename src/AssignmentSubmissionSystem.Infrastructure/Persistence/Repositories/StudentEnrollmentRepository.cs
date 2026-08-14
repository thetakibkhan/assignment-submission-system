using AssignmentSubmissionSystem.Application.AcademicSetup.Enrollments;
using AssignmentSubmissionSystem.Domain.Academics;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Infrastructure.Persistence.Repositories;

public sealed class StudentEnrollmentRepository : IStudentEnrollmentRepository
{
    private readonly ApplicationDbContext _databaseContext;

    public StudentEnrollmentRepository(ApplicationDbContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public async Task AddAsync(StudentEnrollment enrollment, CancellationToken cancellationToken)
    {
        await _databaseContext.StudentEnrollments.AddAsync(enrollment, cancellationToken);
        await _databaseContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExistsActiveAsync(
        Guid studentUserId,
        Guid academicClassId,
        CancellationToken cancellationToken)
    {
        return _databaseContext.StudentEnrollments.AnyAsync(
            enrollment => enrollment.StudentUserId == studentUserId
                && enrollment.AcademicClassId == academicClassId
                && enrollment.EndedAt == null,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetActiveStudentUserIdsAsync(Guid academicClassId, CancellationToken cancellationToken)
    {
        return await (from enrollment in _databaseContext.StudentEnrollments.AsNoTracking()
                      join user in _databaseContext.Users.AsNoTracking() on enrollment.StudentUserId equals user.Id
                      where enrollment.AcademicClassId == academicClassId && enrollment.EndedAt == null && user.IsActive
                      select enrollment.StudentUserId)
            .ToListAsync(cancellationToken);
    }

    public Task<StudentEnrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _databaseContext.StudentEnrollments.SingleOrDefaultAsync(
            enrollment => enrollment.Id == id,
            cancellationToken);
    }

    public async Task UpdateAsync(StudentEnrollment enrollment, CancellationToken cancellationToken)
    {
        _databaseContext.StudentEnrollments.Update(enrollment);
        await _databaseContext.SaveChangesAsync(cancellationToken);
    }
}
