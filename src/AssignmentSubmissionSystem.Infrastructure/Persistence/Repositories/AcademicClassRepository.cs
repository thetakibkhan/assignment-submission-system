using AssignmentSubmissionSystem.Application.AcademicSetup.AcademicClasses;
using AssignmentSubmissionSystem.Domain.Academics;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Infrastructure.Persistence.Repositories;

public sealed class AcademicClassRepository : IAcademicClassRepository
{
    private readonly ApplicationDbContext _databaseContext;

    public AcademicClassRepository(ApplicationDbContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public async Task AddAsync(AcademicClass academicClass, CancellationToken cancellationToken)
    {
        await _databaseContext.AcademicClasses.AddAsync(academicClass, cancellationToken);
        await _databaseContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AcademicClass>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _databaseContext.AcademicClasses
            .AsNoTracking()
            .OrderBy(academicClass => academicClass.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<AcademicClass?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _databaseContext.AcademicClasses.SingleOrDefaultAsync(academicClass => academicClass.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(AcademicClass academicClass, CancellationToken cancellationToken)
    {
        _databaseContext.AcademicClasses.Update(academicClass);
        await _databaseContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return _databaseContext.AcademicClasses.AnyAsync(
            academicClass => academicClass.Code == code,
            cancellationToken);
    }
}
