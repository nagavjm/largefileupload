namespace LargeFileUpload.Domain.ValueObjects;

public sealed record UploadMetadata(
    string OriginalFileName,
    string ContentType,
    long FileSize,
    string? Extension)
{
    public static UploadMetadata Create(string originalFileName, string contentType, long fileSize)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ArgumentException("Original file name cannot be empty.", nameof(originalFileName));

        if (fileSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(fileSize), "File size must be greater than zero bytes.");

        var ext = Path.GetExtension(originalFileName).TrimStart('.').ToLowerInvariant();
        return new UploadMetadata(originalFileName.Trim(), contentType.Trim(), fileSize, ext);
    }
}
