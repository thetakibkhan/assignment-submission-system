namespace AssignmentSubmissionSystem.Application.Submissions;

public interface ISubmissionFileStorage
{
    Task DeleteAsync(string storageName, CancellationToken cancellationToken);

    Task<Stream?> OpenReadAsync(string storageName, CancellationToken cancellationToken);

    Task<StoredSubmissionAttachment> SaveAsync(
        SubmissionAttachmentUpload upload,
        CancellationToken cancellationToken);
}
