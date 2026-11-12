using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using LargeFileUpload.Application.Common.Interfaces;
using LargeFileUpload.Application.Uploads.Dtos;
using LargeFileUpload.Infrastructure.Storage.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LargeFileUpload.Infrastructure.Storage;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClientFactory _clientFactory;
    private readonly AzureStorageOptions _options;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(
        BlobServiceClientFactory clientFactory,
        IOptions<AzureStorageOptions> options,
        ILogger<AzureBlobStorageService> logger)
    {
        _clientFactory = clientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task EnsureContainerExistsAsync(CancellationToken cancellationToken = default)
    {
        var client = _clientFactory.GetClient();
        var containerClient = client.GetBlobContainerClient(_options.ContainerName);

        var response = await containerClient.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);

        if (response != null)
        {
            _logger.LogInformation("Created Azure Blob Container '{ContainerName}' with Private access.", _options.ContainerName);
        }
    }

    public async Task ConfigureCorsAsync(string[] allowedOrigins, CancellationToken cancellationToken = default)
    {
        if (allowedOrigins.Length == 0)
            return;

        try
        {
            var client = _clientFactory.GetClient();
            var properties = await client.GetPropertiesAsync(cancellationToken);

            var corsRule = new BlobCorsRule
            {
                AllowedOrigins = string.Join(",", allowedOrigins),
                AllowedMethods = "GET,PUT,HEAD,OPTIONS,POST,DELETE",
                AllowedHeaders = "*",
                ExposedHeaders = "*",
                MaxAgeInSeconds = 3600
            };

            properties.Value.Cors.Clear();
            properties.Value.Cors.Add(corsRule);

            await client.SetPropertiesAsync(properties.Value, cancellationToken);
            _logger.LogInformation("Successfully configured Azure Storage CORS rules for origins: {Origins}", string.Join(", ", allowedOrigins));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not set Azure Storage CORS properties (may not be supported on this storage emulator endpoint).");
        }
    }

    public async Task<bool> BlobExistsAsync(string blobName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);
        var blobClient = GetBlobClient(blobName);
        var response = await blobClient.ExistsAsync(cancellationToken);
        return response.Value;
    }

    public async Task<BlobPropertiesDto> GetBlobPropertiesAsync(string blobName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);
        var blobClient = GetBlobClient(blobName);

        var properties = (await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken)).Value;

        return new BlobPropertiesDto(
            ContentLength: properties.ContentLength,
            ContentType: properties.ContentType,
            ContentMD5: properties.ContentHash,
            ETag: properties.ETag.ToString(),
            LastModified: properties.LastModified,
            Metadata: new Dictionary<string, string>(properties.Metadata));
    }

    public async Task<BlockListDto> GetBlockListAsync(string blobName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);
        var blockBlobClient = GetBlockBlobClient(blobName);

        try
        {
            var blockList = (await blockBlobClient.GetBlockListAsync(
                BlockListTypes.All,
                cancellationToken: cancellationToken)).Value;

            var committed = blockList.CommittedBlocks
                .Select(b => new BlockInfoDto(b.Name, b.Size, true))
                .ToList();

            var uncommitted = blockList.UncommittedBlocks
                .Select(b => new BlockInfoDto(b.Name, b.Size, false))
                .ToList();

            return new BlockListDto(committed, uncommitted);
        }
        catch (Azure.RequestFailedException ex) when (ex.ErrorCode == "BlobNotFound")
        {
            return new BlockListDto([], []);
        }
    }

    public async Task<BlobCommitResultDto> CommitBlockListAsync(
        string blobName,
        IEnumerable<string> blockIds,
        string contentType,
        IDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);
        var blockBlobClient = GetBlockBlobClient(blobName);

        var options = new CommitBlockListOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            },
            Metadata = metadata
        };

        var response = await blockBlobClient.CommitBlockListAsync(
            blockIds,
            options,
            cancellationToken);

        var properties = (await blockBlobClient.GetPropertiesAsync(cancellationToken: cancellationToken)).Value;

        return new BlobCommitResultDto(
            BlobUri: blockBlobClient.Uri,
            ContentLength: properties.ContentLength,
            ETag: response.Value.ETag.ToString(),
            ContentMD5: response.Value.ContentHash != null ? Convert.ToBase64String(response.Value.ContentHash) : null);
    }

    public async Task<bool> DeleteBlobIfExistsAsync(string blobName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);
        var blobClient = GetBlobClient(blobName);
        var response = await blobClient.DeleteIfExistsAsync(
            DeleteSnapshotsOption.IncludeSnapshots,
            cancellationToken: cancellationToken);
        return response.Value;
    }

    public Uri GetBlobUri(string blobName)
    {
        return GetBlobClient(blobName).Uri;
    }

    private BlobClient GetBlobClient(string blobName)
    {
        var client = _clientFactory.GetClient();
        var containerClient = client.GetBlobContainerClient(_options.ContainerName);
        return containerClient.GetBlobClient(blobName);
    }

    private BlockBlobClient GetBlockBlobClient(string blobName)
    {
        var client = _clientFactory.GetClient();
        var containerClient = client.GetBlobContainerClient(_options.ContainerName);
        return containerClient.GetBlockBlobClient(blobName);
    }
}
