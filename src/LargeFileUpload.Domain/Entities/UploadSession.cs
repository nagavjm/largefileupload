using LargeFileUpload.Domain.Common;
using LargeFileUpload.Domain.Enums;
using LargeFileUpload.Domain.Exceptions;
using LargeFileUpload.Domain.ValueObjects;

namespace LargeFileUpload.Domain.Entities;

public class UploadSession : BaseEntity, IAggregateRoot
{
    public string UserId { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string BlobName { get; private set; } = string.Empty;
    public string ContainerName { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public string ContentType { get; private set; } = string.Empty;
    public UploadStrategy UploadStrategy { get; private set; }
    public int? ChunkSize { get; private set; }
    public int? TotalChunks { get; private set; }
    public int UploadedChunks { get; private set; }
    public UploadStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    // Parameterless constructor for EF Core
    protected UploadSession()
    {
    }

    public static UploadSession CreateDirect(
        string userId,
        UploadMetadata metadata,
        string blobName,
        string containerName,
        TimeSpan sessionDuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ArgumentNullException.ThrowIfNull(metadata);

        var now = DateTimeOffset.UtcNow;
        return new UploadSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OriginalFileName = metadata.OriginalFileName,
            BlobName = blobName,
            ContainerName = containerName,
            FileSize = metadata.FileSize,
            ContentType = metadata.ContentType,
            UploadStrategy = UploadStrategy.DirectSas,
            ChunkSize = null,
            TotalChunks = 1,
            UploadedChunks = 0,
            Status = UploadStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
            ExpiresAt = now.Add(sessionDuration),
            CompletedAt = null,
            ErrorMessage = null
        };
    }

    public static UploadSession CreateChunked(
        string userId,
        UploadMetadata metadata,
        string blobName,
        string containerName,
        int chunkSize,
        TimeSpan sessionDuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ArgumentNullException.ThrowIfNull(metadata);

        if (chunkSize <= 0)
            throw new DomainValidationException("Chunk size must be greater than zero.");

        var totalChunks = (int)Math.Ceiling((double)metadata.FileSize / chunkSize);
        if (totalChunks > 50000)
            throw new DomainValidationException("Total block count exceeds Azure Storage limit of 50,000 blocks.");

        var now = DateTimeOffset.UtcNow;
        return new UploadSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OriginalFileName = metadata.OriginalFileName,
            BlobName = blobName,
            ContainerName = containerName,
            FileSize = metadata.FileSize,
            ContentType = metadata.ContentType,
            UploadStrategy = UploadStrategy.ManualBlockBlob,
            ChunkSize = chunkSize,
            TotalChunks = totalChunks,
            UploadedChunks = 0,
            Status = UploadStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
            ExpiresAt = now.Add(sessionDuration),
            CompletedAt = null,
            ErrorMessage = null
        };
    }

    public void MarkUploading()
    {
        EnsureNotFinished();
        Status = UploadStatus.Uploading;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateProgress(int uploadedChunks)
    {
        EnsureNotFinished();
        if (uploadedChunks < 0)
            throw new DomainValidationException("Uploaded chunks cannot be negative.");

        UploadedChunks = Math.Min(uploadedChunks, TotalChunks ?? uploadedChunks);
        Status = UploadStatus.Uploading;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkProcessing()
    {
        EnsureNotFinished();
        Status = UploadStatus.Processing;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkCompleted(long verifiedBytes)
    {
        EnsureNotFinished();

        if (verifiedBytes != FileSize)
        {
            throw new DomainValidationException(
                $"Verified blob size ({verifiedBytes} bytes) does not match expected file size ({FileSize} bytes).");
        }

        Status = UploadStatus.Completed;
        UploadedChunks = TotalChunks ?? 1;
        CompletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        ErrorMessage = null;
    }

    public void MarkFailed(string errorMessage)
    {
        EnsureNotFinished();
        Status = UploadStatus.Failed;
        ErrorMessage = errorMessage;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel(string? reason = null)
    {
        EnsureNotFinished();
        Status = UploadStatus.Cancelled;
        ErrorMessage = reason ?? "Upload cancelled by user.";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Expire()
    {
        if (Status == UploadStatus.Completed)
            return;

        Status = UploadStatus.Expired;
        ErrorMessage = "Upload session expired.";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsExpired(DateTimeOffset currentUtcNow) =>
        Status != UploadStatus.Completed && currentUtcNow > ExpiresAt;

    private void EnsureNotFinished()
    {
        if (Status is UploadStatus.Completed or UploadStatus.Cancelled or UploadStatus.Expired)
        {
            throw new InvalidUploadStateException(
                $"Cannot modify upload session in '{Status}' state (Id: {Id}).");
        }
    }
}
