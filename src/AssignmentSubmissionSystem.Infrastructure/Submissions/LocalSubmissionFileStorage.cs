using AssignmentSubmissionSystem.Application.Submissions;
using Microsoft.Extensions.Options;

namespace AssignmentSubmissionSystem.Infrastructure.Submissions;

public sealed class LocalSubmissionFileStorage : ISubmissionFileStorage
{
    private readonly string _storageRoot;

    public LocalSubmissionFileStorage(IOptions<SubmissionStorageOptions> options)
    {
        _storageRoot = string.IsNullOrWhiteSpace(options.Value.RootPath)
            ? Path.Combine(Directory.GetCurrentDirectory(), ".local-data", "submissions")
            : options.Value.RootPath;
    }

    public Task DeleteAsync(string storageName, CancellationToken cancellationToken)
    {
        string safeStorageName = Path.GetFileName(storageName);
        if (string.Equals(storageName, safeStorageName, StringComparison.Ordinal))
        {
            File.Delete(Path.Combine(_storageRoot, safeStorageName));
        }

        return Task.CompletedTask;
    }

    public Task<Stream?> OpenReadAsync(string storageName, CancellationToken cancellationToken)
    {
        string safeStorageName = Path.GetFileName(storageName);
        if (!string.Equals(storageName, safeStorageName, StringComparison.Ordinal))
        {
            return Task.FromResult<Stream?>(null);
        }

        string storagePath = Path.Combine(_storageRoot, safeStorageName);
        if (!File.Exists(storagePath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(
            storagePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult<Stream?>(stream);
    }

    public async Task<StoredSubmissionAttachment> SaveAsync(
        SubmissionAttachmentUpload upload,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_storageRoot);
        string extension = Path.GetExtension(upload.OriginalFileName).ToLowerInvariant();
        string storageName = Guid.CreateVersion7() + extension;
        string storagePath = Path.Combine(_storageRoot, storageName);

        await using Stream source = upload.Content;
        await using FileStream destination = File.Create(storagePath);
        await source.CopyToAsync(destination, cancellationToken);

        return new StoredSubmissionAttachment
        {
            ContentType = upload.ContentType,
            OriginalFileName = Path.GetFileName(upload.OriginalFileName),
            StorageName = storageName
        };
    }
}
