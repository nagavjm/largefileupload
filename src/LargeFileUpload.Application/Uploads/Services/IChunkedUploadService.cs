using LargeFileUpload.Application.Uploads.Dtos;

namespace LargeFileUpload.Application.Uploads.Services;

public interface IChunkedUploadService
{
    Task<ChunkedUploadInitiationResponse> InitiateAsync(InitiateChunkedUploadRequest request, CancellationToken cancellationToken = default);

    Task<ChunkedUploadBlocksResponse> GetBlocksAsync(Guid uploadId, CancellationToken cancellationToken = default);

    Task<ChunkedUploadCommitResponse> CommitAsync(Guid uploadId, CommitChunkedUploadRequest request, CancellationToken cancellationToken = default);

    Task<CancelUploadResponse> CancelAsync(Guid uploadId, CancelUploadRequest request, CancellationToken cancellationToken = default);
}
