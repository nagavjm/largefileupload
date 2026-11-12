using LargeFileUpload.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LargeFileUpload.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<UploadSession> UploadSessions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
