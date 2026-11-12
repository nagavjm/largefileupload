using Azure.Core;
using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using LargeFileUpload.Infrastructure.Storage.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LargeFileUpload.Infrastructure.Storage;

public class BlobServiceClientFactory
{
    private readonly AzureStorageOptions _options;
    private readonly ILogger<BlobServiceClientFactory> _logger;
    private readonly Lazy<BlobServiceClient> _lazyClient;
    private readonly StorageSharedKeyCredential? _sharedKeyCredential;

    public BlobServiceClientFactory(
        IOptions<AzureStorageOptions> options,
        ILogger<BlobServiceClientFactory> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (_options.UseAzurite || string.Equals(_options.ConnectionString, "UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase))
        {
            var connStr = string.IsNullOrWhiteSpace(_options.ConnectionString)
                ? "UseDevelopmentStorage=true"
                : _options.ConnectionString;

            _logger.LogInformation("Initializing BlobServiceClient using Azurite / Local Connection String.");
            _lazyClient = new Lazy<BlobServiceClient>(() => new BlobServiceClient(connStr));

            // Extract SharedKeyCredential for local SAS signing
            _sharedKeyCredential = ExtractSharedKeyCredential(connStr);
        }
        else if (!string.IsNullOrWhiteSpace(_options.AccountName) && !string.IsNullOrWhiteSpace(_options.AccountKey))
        {
            _logger.LogInformation("Initializing BlobServiceClient using Azure Storage AccountName '{AccountName}' with AccountKey.", _options.AccountName);
            var serviceUri = new Uri($"https://{_options.AccountName}.blob.core.windows.net");
            _sharedKeyCredential = new StorageSharedKeyCredential(_options.AccountName, _options.AccountKey);
            _lazyClient = new Lazy<BlobServiceClient>(() => new BlobServiceClient(serviceUri, _sharedKeyCredential));
        }
        else if (!string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            _logger.LogInformation("Initializing BlobServiceClient using custom Azure Storage ConnectionString.");
            _lazyClient = new Lazy<BlobServiceClient>(() => new BlobServiceClient(_options.ConnectionString));
            _sharedKeyCredential = ExtractSharedKeyCredential(_options.ConnectionString);
        }
        else if (!string.IsNullOrWhiteSpace(_options.ServiceUri))
        {
            _logger.LogInformation("Initializing BlobServiceClient using DefaultAzureCredential for ServiceUri: {ServiceUri}", _options.ServiceUri);
            var credential = new DefaultAzureCredential();
            _lazyClient = new Lazy<BlobServiceClient>(() => new BlobServiceClient(new Uri(_options.ServiceUri), credential));
            _sharedKeyCredential = null;
        }
        else
        {
            throw new InvalidOperationException("No valid Azure Storage configuration found. Configure 'ConnectionString', 'AccountName' + 'AccountKey', or 'ServiceUri'.");
        }
    }

    public BlobServiceClient GetClient() => _lazyClient.Value;

    public StorageSharedKeyCredential? SharedKeyCredential => _sharedKeyCredential;

    public bool IsLocalDevelopment => IsAzurite;

    public bool IsAzurite => _options.UseAzurite 
        || string.Equals(_options.ConnectionString, "UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase)
        || (_options.ServiceUri != null && _options.ServiceUri.Contains("127.0.0.1"));

    private static StorageSharedKeyCredential? ExtractSharedKeyCredential(string connectionString)
    {
        if (string.Equals(connectionString, "UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase))
        {
            // Well-known Azurite development storage credentials
            return new StorageSharedKeyCredential(
                "devstoreaccount1",
                "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==");
        }

        try
        {
            var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('=', 2))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.OrdinalIgnoreCase);

            if (parts.TryGetValue("AccountName", out var accountName) &&
                parts.TryGetValue("AccountKey", out var accountKey))
            {
                return new StorageSharedKeyCredential(accountName, accountKey);
            }
        }
        catch
        {
            // Ignore parsing errors and return null
        }

        return null;
    }
}
