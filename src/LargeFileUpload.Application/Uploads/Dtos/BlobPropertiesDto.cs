namespace LargeFileUpload.Application.Uploads.Dtos;

public record BlobPropertiesDto(
    long ContentLength,
    string ContentType,
    byte[]? ContentMD5,
    string? ETag,
    DateTimeOffset? LastModified,
    IReadOnlyDictionary<string, string> Metadata);
