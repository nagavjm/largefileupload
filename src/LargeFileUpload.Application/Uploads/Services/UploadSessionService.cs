using LargeFileUpload.Application.Common.Interfaces;
using LargeFileUpload.Application.Common.Models;
using LargeFileUpload.Application.Uploads.Dtos;
using LargeFileUpload.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LargeFileUpload.Application.Uploads.Services;

public class UploadSessionService : IUploadSessionService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UploadSessionService> _logger;

    public UploadSessionService(
        IApplicationDbContext dbContext,
        IBlobStorageService blobStorageService,
        ICurrentUserService currentUserService,
        ILogger<UploadSessionService> logger)
    {
        _dbContext = dbContext;
        _blobStorageService = blobStorageService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<UploadSessionDto?> GetByIdAsync(Guid uploadId, CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.UploadSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == uploadId, cancellationToken);

        if (session == null)
            return null;

        var currentUserId = _currentUserService.UserId ?? "dev-user-001";
        if (!string.Equals(session.UserId, currentUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("You are not authorized to view this upload session.");
        }

        return UploadSessionDto.FromEntity(session);
    }

    public async Task<PaginatedList<UploadSessionDto>> GetUserHistoryAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId ?? "dev-user-001";

        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.UploadSessions
            .AsNoTracking()
            .Where(x => x.UserId == currentUserId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => UploadSessionDto.FromEntity(x))
            .ToListAsync(cancellationToken);

        return new PaginatedList<UploadSessionDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<bool> DeleteUploadAsync(Guid uploadId, CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.UploadSessions
            .FirstOrDefaultAsync(x => x.Id == uploadId, cancellationToken);

        if (session == null)
            return false;

        var currentUserId = _currentUserService.UserId ?? "dev-user-001";
        if (!string.Equals(session.UserId, currentUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("You are not authorized to delete this upload session.");
        }

        // Delete blob in Azure Storage if present
        await _blobStorageService.DeleteBlobIfExistsAsync(session.BlobName, cancellationToken);

        _dbContext.UploadSessions.Remove(session);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted upload session {UploadId} and purged blob '{BlobName}'.", uploadId, session.BlobName);
        return true;
    }
}
