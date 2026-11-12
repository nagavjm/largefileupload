using LargeFileUpload.Application.Uploads.Dtos;

namespace LargeFileUpload.Application.Common.Interfaces;

public interface IBlobStorageService
{
    Task EnsureContainerExistsAsync(CancellationToken cancellationToken = default);

    Task ConfigureCorsAsync(string[] allowedOrigins, CancellationToken cancellationToken = default);

    Task<bool> BlobExistsAsync(string blobName, CancellationToken cancellationToken = default);

    Task<BlobPropertiesDto> GetBlobPropertiesAsync(string blobName, CancellationToken cancellationToken = default);

    Task<BlockListDto> GetBlockListAsync(string blobName, CancellationToken cancellationToken = default);

    Task<BlobCommitResultDto> CommitBlockListAsync(
        string blobName,
        IEnumerable<string> blockIds,
        string contentType,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteBlobIfExistsAsync(string blobName, CancellationToken cancellationToken = default);

    Uri GetBlobUri(string blobName);
}
