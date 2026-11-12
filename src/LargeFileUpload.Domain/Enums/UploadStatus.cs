namespace LargeFileUpload.Domain.Enums;

public enum UploadStatus
{
    Pending = 1,
    Uploading = 2,
    Processing = 3,
    Completed = 4,
    Failed = 5,
    Cancelled = 6,
    Expired = 7
}
