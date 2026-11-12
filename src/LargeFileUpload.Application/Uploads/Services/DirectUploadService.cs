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

public class DirectUploadService : IDirectUploadService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IUserDelegationSasService _sasService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<InitiateDirectUploadRequest> _validator;
    private readonly ILogger<DirectUploadService> _logger;

    public DirectUploadService(
        IApplicationDbContext dbContext,
        IBlobStorageService blobStorageService,
        IUserDelegationSasService sasService,
        ICurrentUserService currentUserService,
        IValidator<InitiateDirectUploadRequest> validator,
        ILogger<DirectUploadService> logger)
    {
        _dbContext = dbContext;
        _blobStorageService = blobStorageService;
        _sasService = sasService;
        _currentUserService = currentUserService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<DirectUploadInitiationResponse> InitiateAsync(
        InitiateDirectUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var userId = _currentUserService.UserId ?? "dev-user-001";
        var metadata = UploadMetadata.Create(request.FileName, request.ContentType, request.FileSize);

        var uploadId = Guid.NewGuid();
        var sanitizedFileName = Path.GetFileName(request.FileName);
        var blobName = $"uploads/{userId}/{DateTime.UtcNow:yyyy/MM/dd}/{uploadId:N}_{sanitizedFileName}";
        const string containerName = "uploads";
        var sessionDuration = TimeSpan.FromMinutes(60);

        // Ensure Azure Storage container exists
        await _blobStorageService.EnsureContainerExistsAsync(cancellationToken);

        // Generate short-lived User Delegation SAS
        var sasUri = await _sasService.GenerateUploadSasUriAsync(blobName, sessionDuration, cancellationToken);

        // Persist upload session in database
        var session = UploadSession.CreateDirect(
            userId: userId,
            metadata: metadata,
            blobName: blobName,
            containerName: containerName,
            sessionDuration: sessionDuration);

        _dbContext.UploadSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Initiated direct upload session {UploadId} for user {UserId} (Blob: {BlobName}, Size: {FileSize} bytes)",
            session.Id, userId, blobName, metadata.FileSize);

        return new DirectUploadInitiationResponse(
            UploadId: session.Id,
            BlobName: session.BlobName,
            ContainerName: session.ContainerName,
            SasUrl: sasUri.ToString(),
            ExpiresAt: session.ExpiresAt,
            UploadStrategy: session.UploadStrategy.ToString());
    }

    public async Task<CompleteDirectUploadResponse> CompleteAsync(
        Guid uploadId,
        CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.UploadSessions
            .FirstOrDefaultAsync(x => x.Id == uploadId, cancellationToken);

        if (session == null)
        {
            throw new KeyNotFoundException($"Upload session '{uploadId}' was not found.");
        }

        var currentUserId = _currentUserService.UserId ?? "dev-user-001";
        if (!string.Equals(session.UserId, currentUserId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Access denied: User {UserId} attempted to complete session {UploadId} owned by {OwnerId}",
                currentUserId, uploadId, session.UserId);
            throw new UnauthorizedAccessException("You are not authorized to complete this upload session.");
        }

        // Idempotency: If already completed, return existing success state
        if (session.Status == UploadStatus.Completed)
        {
            return new CompleteDirectUploadResponse(
                UploadId: session.Id,
                Status: session.Status.ToString(),
                BlobUri: _blobStorageService.GetBlobUri(session.BlobName).ToString(),
                VerifiedBytes: session.FileSize,
                CompletedAt: session.CompletedAt ?? session.UpdatedAt);
        }

        // Verify blob existence in Azure Storage
        var exists = await _blobStorageService.BlobExistsAsync(session.BlobName, cancellationToken);
        if (!exists)
        {
            _logger.LogError("Upload completion failed: Blob '{BlobName}' does not exist in Azure Storage.", session.BlobName);
            session.MarkFailed("Blob does not exist in Azure Storage.");
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Uploaded file was not found in Azure Storage. The direct upload may have failed or been interrupted.");
        }

        // Verify blob properties and file size
        var properties = await _blobStorageService.GetBlobPropertiesAsync(session.BlobName, cancellationToken);

        session.MarkCompleted(properties.ContentLength);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully completed direct upload session {UploadId}. Verified size: {VerifiedBytes} bytes.",
            session.Id, properties.ContentLength);

        return new CompleteDirectUploadResponse(
            UploadId: session.Id,
            Status: session.Status.ToString(),
            BlobUri: _blobStorageService.GetBlobUri(session.BlobName).ToString(),
            VerifiedBytes: properties.ContentLength,
            CompletedAt: session.CompletedAt ?? DateTimeOffset.UtcNow);
    }
}
