using AssignmentSubmissionSystem.Domain.Academics;

namespace AssignmentSubmissionSystem.Application.AcademicSetup.AcademicClasses;

public interface IAcademicClassService
{
    Task<AcademicClass> ArchiveAsync(Guid id, CancellationToken cancellationToken);

    Task<AcademicClass> CreateAsync(CreateAcademicClassCommand command, CancellationToken cancellationToken);

    Task<IReadOnlyList<AcademicClass>> GetAllAsync(CancellationToken cancellationToken);

    Task<AcademicClass> UpdateAsync(Guid id, CreateAcademicClassCommand command, CancellationToken cancellationToken);
}
