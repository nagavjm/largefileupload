namespace LargeFileUpload.Application.Uploads.Dtos;

public record BlobCommitResultDto(
    Uri BlobUri,
    long ContentLength,
    string? ETag,
    string? ContentMD5);
