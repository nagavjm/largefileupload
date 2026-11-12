namespace LargeFileUpload.Application.Uploads.Dtos;

public record InitiateChunkedUploadRequest(
    string FileName,
    long FileSize,
    string ContentType,
    int? ChunkSize = 4194304); // Default 4 MB

public record ChunkedUploadInitiationResponse(
    Guid UploadId,
    string BlobName,
    string ContainerName,
    string SasUrl,
    int ChunkSize,
    int TotalChunks,
    IReadOnlyList<string> BlockIds,
    DateTimeOffset ExpiresAt,
    string UploadStrategy = "ManualBlockBlob");

public record ChunkedUploadBlocksResponse(
    Guid UploadId,
    int TotalChunks,
    int StagedBlockCount,
    IReadOnlyList<string> StagedBlockIds,
    IReadOnlyList<int> MissingBlockIndexes,
    bool IsResumable);

public record CommitChunkedUploadRequest(
    IReadOnlyList<string> BlockIds);

public record ChunkedUploadCommitResponse(
    Guid UploadId,
    string Status,
    string BlobUri,
    int TotalBlocksCommitted,
    long VerifiedBytes,
    DateTimeOffset CompletedAt);

public record CancelUploadRequest(
    string? Reason);

public record CancelUploadResponse(
    Guid UploadId,
    string Status,
    string Reason);
