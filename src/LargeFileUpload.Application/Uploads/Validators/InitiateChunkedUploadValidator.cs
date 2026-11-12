using FluentValidation;
using LargeFileUpload.Application.Uploads.Dtos;

namespace LargeFileUpload.Application.Uploads.Validators;

public class InitiateChunkedUploadValidator : AbstractValidator<InitiateChunkedUploadRequest>
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp",
        ".pdf", ".docx", ".xlsx", ".pptx", ".txt", ".csv",
        ".zip", ".tar", ".gz", ".7z", ".iso",
        ".mp4", ".mkv", ".mov", ".avi", ".mp3", ".wav",
        ".json", ".xml"
    };

    public const long DefaultMaxFileSize = 53687091200; // 50 GB
    public const int MinChunkSize = 1048576; // 1 MB
    public const int MaxChunkSize = 104857600; // 100 MB

    public InitiateChunkedUploadValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.")
            .MaximumLength(255).WithMessage("File name cannot exceed 255 characters.")
            .Must(NotContainPathTraversal).WithMessage("File name contains invalid path traversal characters.")
            .Must(HaveAllowedExtension).WithMessage(f => $"File extension is not permitted. Allowed: {string.Join(", ", AllowedExtensions)}");

        RuleFor(x => x.FileSize)
            .GreaterThan(0).WithMessage("File size must be greater than 0 bytes.")
            .LessThanOrEqualTo(DefaultMaxFileSize).WithMessage($"File size cannot exceed {DefaultMaxFileSize / 1024 / 1024 / 1024} GB.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("Content-Type is required.")
            .MaximumLength(100).WithMessage("Content-Type cannot exceed 100 characters.");

        RuleFor(x => x.ChunkSize)
            .Must(c => !c.HasValue || (c.Value >= MinChunkSize && c.Value <= MaxChunkSize))
            .WithMessage($"Chunk size must be between {MinChunkSize / 1024 / 1024} MB and {MaxChunkSize / 1024 / 1024} MB.");

        RuleFor(x => x)
            .Must(HaveValidTotalBlocks)
            .WithMessage("Total calculated blocks exceeds the Azure Storage limit of 50,000 blocks. Increase chunk size.");
    }

    private static bool HaveValidTotalBlocks(InitiateChunkedUploadRequest req)
    {
        var chunkSize = req.ChunkSize ?? 4194304;
        if (chunkSize <= 0) return false;
        var blocks = (long)Math.Ceiling((double)req.FileSize / chunkSize);
        return blocks <= 50000;
    }

    private static bool NotContainPathTraversal(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return false;
        return !fileName.Contains("..") && !fileName.Contains('/') && !fileName.Contains('\\');
    }

    private static bool HaveAllowedExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return false;
        var ext = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(ext) && AllowedExtensions.Contains(ext);
    }
}
