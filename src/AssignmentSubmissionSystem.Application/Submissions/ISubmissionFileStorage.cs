namespace AssignmentSubmissionSystem.Application.Submissions;

public interface ISubmissionFileStorage
{
    Task<Stream?> OpenReadAsync(string storageName, CancellationToken cancellationToken);

    Task<StoredSubmissionAttachment> SaveAsync(
        SubmissionAttachmentUpload upload,
        CancellationToken cancellationToken);
}
