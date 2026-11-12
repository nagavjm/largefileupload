using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using LargeFileUpload.Application.Common.Interfaces;
using LargeFileUpload.Infrastructure.Storage.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LargeFileUpload.Infrastructure.Storage;

public class UserDelegationSasService : IUserDelegationSasService
{
    private readonly BlobServiceClientFactory _clientFactory;
    private readonly IMemoryCache _memoryCache;
    private readonly AzureStorageOptions _options;
    private readonly ILogger<UserDelegationSasService> _logger;
    private static readonly SemaphoreSlim UserDelegationKeyLock = new(1, 1);

    public UserDelegationSasService(
        BlobServiceClientFactory clientFactory,
        IMemoryCache memoryCache,
        IOptions<AzureStorageOptions> options,
        ILogger<UserDelegationSasService> logger)
    {
        _clientFactory = clientFactory;
        _memoryCache = memoryCache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Uri> GenerateUploadSasUriAsync(
        string blobName,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        var permissions = BlobSasPermissions.Create |
                          BlobSasPermissions.Write |
                          BlobSasPermissions.Add |
                          BlobSasPermissions.Read;

        return await GenerateSasUriInternalAsync(blobName, permissions, duration, cancellationToken);
    }

    public async Task<Uri> GenerateReadOnlySasUriAsync(
        string blobName,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        var permissions = BlobSasPermissions.Read;
        return await GenerateSasUriInternalAsync(blobName, permissions, duration, cancellationToken);
    }

    private async Task<Uri> GenerateSasUriInternalAsync(
        string blobName,
        BlobSasPermissions permissions,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        var client = _clientFactory.GetClient();
        var containerClient = client.GetBlobContainerClient(_options.ContainerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        var startsOn = DateTimeOffset.UtcNow.AddMinutes(-5); // Clock skew protection
        var expiresOn = DateTimeOffset.UtcNow.Add(duration);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _options.ContainerName,
            BlobName = blobName,
            Resource = "b",
            StartsOn = startsOn,
            ExpiresOn = expiresOn,
            Protocol = _clientFactory.IsLocalDevelopment ? SasProtocol.None : SasProtocol.Https
        };

        sasBuilder.SetPermissions(permissions);

        if (_clientFactory.IsLocalDevelopment && _clientFactory.SharedKeyCredential != null)
        {
            _logger.LogDebug("Signing SAS using local SharedKeyCredential for blob '{BlobName}'.", blobName);
            var sasQueryParameters = sasBuilder.ToSasQueryParameters(_clientFactory.SharedKeyCredential);
            var uriBuilder = new BlobUriBuilder(blobClient.Uri)
            {
                Sas = sasQueryParameters
            };
            return uriBuilder.ToUri();
        }

        // Production Mode: Microsoft Entra ID User Delegation Key
        _logger.LogDebug("Signing SAS using Microsoft Entra ID User Delegation Key for blob '{BlobName}'.", blobName);
        var delegationKey = await GetOrFetchUserDelegationKeyAsync(client, cancellationToken);
        var userDelegationSas = sasBuilder.ToSasQueryParameters(delegationKey, client.AccountName);

        var prodUriBuilder = new BlobUriBuilder(blobClient.Uri)
        {
            Sas = userDelegationSas
        };

        return prodUriBuilder.ToUri();
    }

    private async Task<UserDelegationKey> GetOrFetchUserDelegationKeyAsync(
        BlobServiceClient client,
        CancellationToken cancellationToken)
    {
        const string cacheKey = "AzureStorage_UserDelegationKey";

        if (_memoryCache.TryGetValue(cacheKey, out UserDelegationKey? cachedKey) && cachedKey != null)
        {
            return cachedKey;
        }

        await UserDelegationKeyLock.WaitAsync(cancellationToken);
        try
        {
            if (_memoryCache.TryGetValue(cacheKey, out cachedKey) && cachedKey != null)
            {
                return cachedKey;
            }

            var keyStartsOn = DateTimeOffset.UtcNow.AddMinutes(-5);
            var keyExpiresOn = DateTimeOffset.UtcNow.AddHours(24); // Request 24h key from Azure

            _logger.LogInformation("Requesting new User Delegation Key from Microsoft Entra ID.");
            var response = await client.GetUserDelegationKeyAsync(keyStartsOn, keyExpiresOn, cancellationToken);
            var freshKey = response.Value;

            // Cache key until 10 minutes before actual expiration
            var cacheDuration = (freshKey.SignedExpiresOn - DateTimeOffset.UtcNow).Subtract(TimeSpan.FromMinutes(10));
            if (cacheDuration > TimeSpan.Zero)
            {
                _memoryCache.Set(cacheKey, freshKey, cacheDuration);
            }

            return freshKey;
        }
        finally
        {
            UserDelegationKeyLock.Release();
        }
    }
}
