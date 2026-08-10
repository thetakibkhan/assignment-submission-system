using AssignmentSubmissionSystem.Application.AcademicSetup.ClassCourses;
using AssignmentSubmissionSystem.Domain.Academics;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Infrastructure.Persistence.Repositories;

public sealed class ClassCourseRepository : IClassCourseRepository
{
    private readonly ApplicationDbContext _databaseContext;

    public ClassCourseRepository(ApplicationDbContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public async Task AddAsync(ClassCourse classCourse, CancellationToken cancellationToken)
    {
        await _databaseContext.ClassCourses.AddAsync(classCourse, cancellationToken);
        await _databaseContext.SaveChangesAsync(cancellationToken);
    }

    public Task<ClassCourse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _databaseContext.ClassCourses.SingleOrDefaultAsync(classCourse => classCourse.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(ClassCourse classCourse, CancellationToken cancellationToken)
    {
        _databaseContext.ClassCourses.Update(classCourse);
        await _databaseContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return _databaseContext.ClassCourses.AnyAsync(
            classCourse => classCourse.Code == code,
            cancellationToken);
    }
}
