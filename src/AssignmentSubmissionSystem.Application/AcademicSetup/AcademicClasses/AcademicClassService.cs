using AssignmentSubmissionSystem.Domain.Academics;

namespace AssignmentSubmissionSystem.Application.AcademicSetup.AcademicClasses;

public sealed class AcademicClassService : IAcademicClassService
{
    private readonly IAcademicClassRepository _academicClassRepository;

    public AcademicClassService(IAcademicClassRepository academicClassRepository)
    {
        _academicClassRepository = academicClassRepository;
    }

    public async Task<AcademicClass> ArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        AcademicClass academicClass = await GetRequiredAsync(id, cancellationToken);
        academicClass.Archive();
        await _academicClassRepository.UpdateAsync(academicClass, cancellationToken);
        return academicClass;
    }

    public async Task<AcademicClass> CreateAsync(
        CreateAcademicClassCommand command,
        CancellationToken cancellationToken)
    {
        string code = command.Code.Trim().ToUpperInvariant();

        if (await _academicClassRepository.ExistsByCodeAsync(code, cancellationToken))
        {
            throw new DuplicateAcademicClassCodeException(code);
        }

        AcademicClass academicClass = new(Guid.CreateVersion7(), command.Name, code);
        await _academicClassRepository.AddAsync(academicClass, cancellationToken);

        return academicClass;
    }

    public Task<IReadOnlyList<AcademicClass>> GetAllAsync(CancellationToken cancellationToken)
    {
        return _academicClassRepository.GetAllAsync(cancellationToken);
    }

    public async Task<AcademicClass> UpdateAsync(Guid id, CreateAcademicClassCommand command, CancellationToken cancellationToken)
    {
        AcademicClass academicClass = await GetRequiredAsync(id, cancellationToken);
        string code = command.Code.Trim().ToUpperInvariant();

        if (academicClass.Code != code && await _academicClassRepository.ExistsByCodeAsync(code, cancellationToken))
        {
            throw new DuplicateAcademicClassCodeException(code);
        }

        academicClass.Update(command.Name, code);
        await _academicClassRepository.UpdateAsync(academicClass, cancellationToken);
        return academicClass;
    }

    private async Task<AcademicClass> GetRequiredAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _academicClassRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("The requested Class was not found.");
    }
}
