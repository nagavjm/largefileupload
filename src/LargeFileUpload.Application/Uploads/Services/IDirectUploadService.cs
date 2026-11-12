using LargeFileUpload.Application.Uploads.Dtos;

namespace LargeFileUpload.Application.Uploads.Services;

public interface IDirectUploadService
{
    Task<DirectUploadInitiationResponse> InitiateAsync(InitiateDirectUploadRequest request, CancellationToken cancellationToken = default);

    Task<CompleteDirectUploadResponse> CompleteAsync(Guid uploadId, CancellationToken cancellationToken = default);
}
