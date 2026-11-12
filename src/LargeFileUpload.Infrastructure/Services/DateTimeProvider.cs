using LargeFileUpload.Application.Common.Interfaces;

namespace LargeFileUpload.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
