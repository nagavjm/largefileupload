namespace LargeFileUpload.Application.Common.Interfaces;

public interface IUserDelegationSasService
{
    Task<Uri> GenerateUploadSasUriAsync(string blobName, TimeSpan duration, CancellationToken cancellationToken = default);

    Task<Uri> GenerateReadOnlySasUriAsync(string blobName, TimeSpan duration, CancellationToken cancellationToken = default);
}
