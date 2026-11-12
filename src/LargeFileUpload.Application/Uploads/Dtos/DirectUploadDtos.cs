namespace LargeFileUpload.Application.Uploads.Dtos;

public record InitiateDirectUploadRequest(
    string FileName,
    long FileSize,
    string ContentType);

public record DirectUploadInitiationResponse(
    Guid UploadId,
    string BlobName,
    string ContainerName,
    string SasUrl,
    DateTimeOffset ExpiresAt,
    string UploadStrategy,
    long MaxBlockSizeBytes = 8388608); // 8 MB default blockSize for browser data
