using LargeFileUpload.Application.Common.Models;
using LargeFileUpload.Application.Uploads.Dtos;

namespace LargeFileUpload.Application.Uploads.Services;

public interface IUploadSessionService
{
    Task<UploadSessionDto?> GetByIdAsync(Guid uploadId, CancellationToken cancellationToken = default);

    Task<PaginatedList<UploadSessionDto>> GetUserHistoryAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<bool> DeleteUploadAsync(Guid uploadId, CancellationToken cancellationToken = default);
}
