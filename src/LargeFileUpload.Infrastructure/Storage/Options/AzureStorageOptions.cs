using System.ComponentModel.DataAnnotations;

namespace LargeFileUpload.Infrastructure.Storage.Options;

public class AzureStorageOptions
{
    public const string SectionName = "AzureStorage";

    public string? ServiceUri { get; set; }

    public string? AccountName { get; set; }

    public string? AccountKey { get; set; }

    [Required]
    public string ContainerName { get; set; } = "uploads";

    public string? ConnectionString { get; set; }

    public bool UseAzurite { get; set; } = true;

    [Range(1, 1440)]
    public int SasExpiryMinutes { get; set; } = 60;

    [Range(1, long.MaxValue)]
    public long MaxFileSizeBytes { get; set; } = 53687091200; // 50 GB default

    public string[] AllowedExtensions { get; set; } =
    [
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp",
        ".pdf", ".docx", ".xlsx", ".pptx", ".txt", ".csv",
        ".zip", ".tar", ".gz", ".7z", ".iso",
        ".mp4", ".mkv", ".mov", ".avi", ".mp3", ".wav",
        ".json", ".xml"
    ];

    public string[] CorsAllowedOrigins { get; set; } =
    [
        "http://localhost:4200",
        "https://localhost:4200"
    ];
}
