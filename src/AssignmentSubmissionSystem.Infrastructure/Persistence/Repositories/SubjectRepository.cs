using AssignmentSubmissionSystem.Application.AcademicSetup.Subjects;
using AssignmentSubmissionSystem.Domain.Academics;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Infrastructure.Persistence.Repositories;

public sealed class SubjectRepository : ISubjectRepository
{
    private readonly ApplicationDbContext _databaseContext;

    public SubjectRepository(ApplicationDbContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public async Task AddAsync(Subject subject, CancellationToken cancellationToken)
    {
        await _databaseContext.Subjects.AddAsync(subject, cancellationToken);
        await _databaseContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return _databaseContext.Subjects.AnyAsync(subject => subject.Code == code, cancellationToken);
    }

    public Task<Subject?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _databaseContext.Subjects.SingleOrDefaultAsync(subject => subject.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(Subject subject, CancellationToken cancellationToken)
    {
        _databaseContext.Subjects.Update(subject);
        await _databaseContext.SaveChangesAsync(cancellationToken);
    }
}
