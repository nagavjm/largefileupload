namespace LargeFileUpload.Domain.Exceptions;

public class InvalidUploadStateException : Exception
{
    public InvalidUploadStateException(string message) : base(message)
    {
    }
}
