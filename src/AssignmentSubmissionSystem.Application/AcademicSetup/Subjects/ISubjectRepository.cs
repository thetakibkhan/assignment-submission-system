using AssignmentSubmissionSystem.Domain.Academics;

namespace AssignmentSubmissionSystem.Application.AcademicSetup.Subjects;

public interface ISubjectRepository
{
    Task AddAsync(Subject subject, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken);

    Task<Subject?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task UpdateAsync(Subject subject, CancellationToken cancellationToken);
}
