using FluentValidation;
using LargeFileUpload.Application.Uploads.Dtos;
using LargeFileUpload.Domain.Exceptions;

namespace LargeFileUpload.Application.Uploads.Validators;

public class InitiateDirectUploadValidator : AbstractValidator<InitiateDirectUploadRequest>
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

    public InitiateDirectUploadValidator()
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
