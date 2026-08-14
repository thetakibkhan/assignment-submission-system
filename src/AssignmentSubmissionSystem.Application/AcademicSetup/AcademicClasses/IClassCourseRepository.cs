using AssignmentSubmissionSystem.Domain.Academics;

namespace AssignmentSubmissionSystem.Application.AcademicSetup.AcademicClasses;

public interface IAcademicClassRepository
{
    Task AddAsync(AcademicClass academicClass, CancellationToken cancellationToken);

    Task<IReadOnlyList<AcademicClass>> GetAllAsync(CancellationToken cancellationToken);

    Task<AcademicClass?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task UpdateAsync(AcademicClass academicClass, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken);
}
