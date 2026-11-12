using LargeFileUpload.Domain.Entities;

namespace LargeFileUpload.Application.Uploads.Dtos;

public record CompleteDirectUploadResponse(
    Guid UploadId,
    string Status,
    string BlobUri,
    long VerifiedBytes,
    DateTimeOffset CompletedAt);

public record UploadSessionDto(
    Guid Id,
    string UserId,
    string OriginalFileName,
    string BlobName,
    string ContainerName,
    long FileSize,
    string ContentType,
    string UploadStrategy,
    int? ChunkSize,
    int? TotalChunks,
    int UploadedChunks,
    string Status,
    double ProgressPercentage,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? CompletedAt,
    string? ErrorMessage)
{
    public static UploadSessionDto FromEntity(UploadSession entity)
    {
        var percentage = entity.FileSize > 0 && entity.TotalChunks.HasValue && entity.TotalChunks > 0
            ? Math.Round(((double)entity.UploadedChunks / entity.TotalChunks.Value) * 100, 2)
            : entity.Status == Domain.Enums.UploadStatus.Completed ? 100.0 : 0.0;

        return new UploadSessionDto(
            Id: entity.Id,
            UserId: entity.UserId,
            OriginalFileName: entity.OriginalFileName,
            BlobName: entity.BlobName,
            ContainerName: entity.ContainerName,
            FileSize: entity.FileSize,
            ContentType: entity.ContentType,
            UploadStrategy: entity.UploadStrategy.ToString(),
            ChunkSize: entity.ChunkSize,
            TotalChunks: entity.TotalChunks,
            UploadedChunks: entity.UploadedChunks,
            Status: entity.Status.ToString(),
            ProgressPercentage: percentage,
            CreatedAt: entity.CreatedAt,
            UpdatedAt: entity.UpdatedAt,
            ExpiresAt: entity.ExpiresAt,
            CompletedAt: entity.CompletedAt,
            ErrorMessage: entity.ErrorMessage);
    }
}
