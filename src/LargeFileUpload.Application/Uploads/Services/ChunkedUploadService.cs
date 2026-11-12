using System.Text;
using FluentValidation;
using LargeFileUpload.Application.Common.Interfaces;
using LargeFileUpload.Application.Uploads.Dtos;
using LargeFileUpload.Domain.Entities;
using LargeFileUpload.Domain.Enums;
using LargeFileUpload.Domain.Exceptions;
using LargeFileUpload.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LargeFileUpload.Application.Uploads.Services;

public class ChunkedUploadService : IChunkedUploadService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IUserDelegationSasService _sasService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<InitiateChunkedUploadRequest> _initiateValidator;
    private readonly IValidator<CommitChunkedUploadRequest> _commitValidator;
    private readonly ILogger<ChunkedUploadService> _logger;

    public ChunkedUploadService(
        IApplicationDbContext dbContext,
        IBlobStorageService blobStorageService,
        IUserDelegationSasService sasService,
        ICurrentUserService currentUserService,
        IValidator<InitiateChunkedUploadRequest> initiateValidator,
        IValidator<CommitChunkedUploadRequest> commitValidator,
        ILogger<ChunkedUploadService> logger)
    {
        _dbContext = dbContext;
        _blobStorageService = blobStorageService;
        _sasService = sasService;
        _currentUserService = currentUserService;
        _initiateValidator = initiateValidator;
        _commitValidator = commitValidator;
        _logger = logger;
    }

    public async Task<ChunkedUploadInitiationResponse> InitiateAsync(
        InitiateChunkedUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _initiateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var userId = _currentUserService.UserId ?? "dev-user-001";
        var chunkSize = request.ChunkSize ?? 4194304; // 4 MB default
        var metadata = UploadMetadata.Create(request.FileName, request.ContentType, request.FileSize);

        var uploadId = Guid.NewGuid();
        var sanitizedFileName = Path.GetFileName(request.FileName);
        var blobName = $"uploads/{userId}/{DateTime.UtcNow:yyyy/MM/dd}/{uploadId:N}_{sanitizedFileName}";
        const string containerName = "uploads";
        var sessionDuration = TimeSpan.FromHours(2); // 2 hours for large multi-part uploads

        // Ensure container exists
        await _blobStorageService.EnsureContainerExistsAsync(cancellationToken);

        // Pre-generate deterministic Base64 Block IDs
        var totalChunks = (int)Math.Ceiling((double)request.FileSize / chunkSize);
        var blockIds = GenerateDeterministicBlockIds(totalChunks);

        // Generate short-lived User Delegation SAS URL for staging blocks
        var sasUri = await _sasService.GenerateUploadSasUriAsync(blobName, sessionDuration, cancellationToken);

        // Persist session
        var session = UploadSession.CreateChunked(
            userId: userId,
            metadata: metadata,
            blobName: blobName,
            containerName: containerName,
            chunkSize: chunkSize,
            sessionDuration: sessionDuration);

        // Set status to Uploading right away
        session.MarkUploading();

        _dbContext.UploadSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Initiated chunked upload session {UploadId} for user {UserId}. FileSize: {FileSize}, ChunkSize: {ChunkSize}, TotalChunks: {TotalChunks}",
            session.Id, userId, metadata.FileSize, chunkSize, totalChunks);

        return new ChunkedUploadInitiationResponse(
            UploadId: session.Id,
            BlobName: session.BlobName,
            ContainerName: session.ContainerName,
            SasUrl: sasUri.ToString(),
            ChunkSize: chunkSize,
            TotalChunks: totalChunks,
            BlockIds: blockIds,
            ExpiresAt: session.ExpiresAt);
    }

    public async Task<ChunkedUploadBlocksResponse> GetBlocksAsync(
        Guid uploadId,
        CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.UploadSessions
            .FirstOrDefaultAsync(x => x.Id == uploadId, cancellationToken);

        if (session == null)
        {
            throw new KeyNotFoundException($"Upload session '{uploadId}' was not found.");
        }

        ValidateOwnership(session);

        var totalChunks = session.TotalChunks ?? 1;
        var expectedBlockIds = GenerateDeterministicBlockIds(totalChunks);

        // Query Azure Blob Storage for staged blocks (uncommitted and committed)
        var blockList = await _blobStorageService.GetBlockListAsync(session.BlobName, cancellationToken);
        var stagedBlockIdSet = new HashSet<string>(
            blockList.UncommittedBlocks.Select(b => b.Name)
                .Concat(blockList.CommittedBlocks.Select(b => b.Name)),
            StringComparer.Ordinal);

        var missingIndexes = new List<int>();
        for (var i = 0; i < expectedBlockIds.Count; i++)
        {
            if (!stagedBlockIdSet.Contains(expectedBlockIds[i]))
            {
                missingIndexes.Add(i);
            }
        }

        var stagedCount = totalChunks - missingIndexes.Count;
        if (session.Status == UploadStatus.Uploading && stagedCount != session.UploadedChunks)
        {
            session.UpdateProgress(stagedCount);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var isResumable = session.Status != UploadStatus.Completed &&
                          session.Status != UploadStatus.Cancelled &&
                          !session.IsExpired(DateTimeOffset.UtcNow);

        return new ChunkedUploadBlocksResponse(
            UploadId: session.Id,
            TotalChunks: totalChunks,
            StagedBlockCount: stagedCount,
            StagedBlockIds: stagedBlockIdSet.ToList(),
            MissingBlockIndexes: missingIndexes,
            IsResumable: isResumable);
    }

    public async Task<ChunkedUploadCommitResponse> CommitAsync(
        Guid uploadId,
        CommitChunkedUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _commitValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var session = await _dbContext.UploadSessions
            .FirstOrDefaultAsync(x => x.Id == uploadId, cancellationToken);

        if (session == null)
        {
            throw new KeyNotFoundException($"Upload session '{uploadId}' was not found.");
        }

        ValidateOwnership(session);

        // Idempotency: If already committed, return success
        if (session.Status == UploadStatus.Completed)
        {
            return new ChunkedUploadCommitResponse(
                UploadId: session.Id,
                Status: session.Status.ToString(),
                BlobUri: _blobStorageService.GetBlobUri(session.BlobName).ToString(),
                TotalBlocksCommitted: session.TotalChunks ?? 1,
                VerifiedBytes: session.FileSize,
                CompletedAt: session.CompletedAt ?? session.UpdatedAt);
        }

        if (session.Status is UploadStatus.Cancelled or UploadStatus.Expired)
        {
            throw new InvalidUploadStateException($"Cannot commit upload session in '{session.Status}' state.");
        }

        var totalChunks = session.TotalChunks ?? 1;
        if (request.BlockIds.Count != totalChunks)
        {
            throw new DomainValidationException(
                $"Received block count ({request.BlockIds.Count}) does not match expected total chunk count ({totalChunks}).");
        }

        // Validate block IDs match expected deterministic sequence
        var expectedBlockIds = GenerateDeterministicBlockIds(totalChunks);
        for (var i = 0; i < totalChunks; i++)
        {
            if (!string.Equals(request.BlockIds[i], expectedBlockIds[i], StringComparison.Ordinal))
            {
                throw new DomainValidationException(
                    $"Block ID at index {i} ('{request.BlockIds[i]}') does not match expected block ID ('{expectedBlockIds[i]}').");
            }
        }

        session.MarkProcessing();
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Committing {BlockCount} blocks for session {UploadId}...", request.BlockIds.Count, uploadId);

        // Commit Block List in Azure Blob Storage
        var commitResult = await _blobStorageService.CommitBlockListAsync(
            blobName: session.BlobName,
            blockIds: request.BlockIds,
            contentType: session.ContentType,
            metadata: null,
            cancellationToken: cancellationToken);

        // Final verification of byte length
        session.MarkCompleted(commitResult.ContentLength);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully committed chunked upload {UploadId}. Verified size: {VerifiedBytes} bytes.",
            session.Id, commitResult.ContentLength);

        return new ChunkedUploadCommitResponse(
            UploadId: session.Id,
            Status: session.Status.ToString(),
            BlobUri: commitResult.BlobUri.ToString(),
            TotalBlocksCommitted: request.BlockIds.Count,
            VerifiedBytes: commitResult.ContentLength,
            CompletedAt: session.CompletedAt ?? DateTimeOffset.UtcNow);
    }

    public async Task<CancelUploadResponse> CancelAsync(
        Guid uploadId,
        CancelUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.UploadSessions
            .FirstOrDefaultAsync(x => x.Id == uploadId, cancellationToken);

        if (session == null)
        {
            throw new KeyNotFoundException($"Upload session '{uploadId}' was not found.");
        }

        ValidateOwnership(session);

        var reason = request.Reason ?? "Cancelled by user";

        if (session.Status == UploadStatus.Cancelled)
        {
            return new CancelUploadResponse(
                UploadId: session.Id,
                Status: session.Status.ToString(),
                Reason: session.ErrorMessage ?? reason);
        }

        session.Cancel(reason);

        // Purge uncommitted blocks / blob in Azure Storage
        await _blobStorageService.DeleteBlobIfExistsAsync(session.BlobName, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cancelled upload session {UploadId}. Reason: {Reason}", uploadId, reason);

        return new CancelUploadResponse(
            UploadId: session.Id,
            Status: session.Status.ToString(),
            Reason: reason);
    }

    private void ValidateOwnership(UploadSession session)
    {
        var currentUserId = _currentUserService.UserId ?? "dev-user-001";
        if (!string.Equals(session.UserId, currentUserId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Access denied: User {UserId} attempted action on session {UploadId} owned by {OwnerId}",
                currentUserId, session.Id, session.UserId);
            throw new UnauthorizedAccessException("You are not authorized to manage this upload session.");
        }
    }

    public static IReadOnlyList<string> GenerateDeterministicBlockIds(int totalChunks)
    {
        var blockIds = new string[totalChunks];
        for (var i = 0; i < totalChunks; i++)
        {
            // Deterministic 16-byte base64 block ID (e.g. block-000000 -> YmxvY2stMDAwMDAw)
            blockIds[i] = Convert.ToBase64String(Encoding.UTF8.GetBytes($"block-{i:D6}"));
        }
        return blockIds;
    }
}
