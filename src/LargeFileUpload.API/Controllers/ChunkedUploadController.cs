using LargeFileUpload.Application.Uploads.Dtos;
using LargeFileUpload.Application.Uploads.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LargeFileUpload.API.Controllers;

[ApiController]
[Route("api/uploads/chunked")]
[Produces("application/json")]
public class ChunkedUploadController : BaseApiController
{
    private readonly IChunkedUploadService _chunkedUploadService;
    private readonly ILogger<ChunkedUploadController> _logger;

    public ChunkedUploadController(
        IChunkedUploadService chunkedUploadService,
        ILogger<ChunkedUploadController> logger)
    {
        _chunkedUploadService = chunkedUploadService;
        _logger = logger;
    }

    /// <summary>
    /// Initiates a chunked block-blob upload session, pre-generating deterministic Block IDs and a restricted SAS URL.
    /// </summary>
    [HttpPost("initiate")]
    [EnableRateLimiting("UploadInitiatePolicy")]
    [ProducesResponseType(typeof(ChunkedUploadInitiationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChunkedUploadInitiationResponse>> Initiate(
        [FromBody] InitiateChunkedUploadRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initiating chunked upload for '{FileName}' ({FileSize} bytes).", request.FileName, request.FileSize);
        var response = await _chunkedUploadService.InitiateAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Queries Azure Blob Storage and session state to retrieve staged blocks for resumability and validation.
    /// </summary>
    [HttpGet("{uploadId:guid}/blocks")]
    [ProducesResponseType(typeof(ChunkedUploadBlocksResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChunkedUploadBlocksResponse>> GetBlocks(
        [FromRoute] Guid uploadId,
        CancellationToken cancellationToken)
    {
        var response = await _chunkedUploadService.GetBlocksAsync(uploadId, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Commits the ordered list of uploaded Block IDs in Azure Blob Storage and marks the session Completed.
    /// </summary>
    [HttpPost("{uploadId:guid}/commit")]
    [ProducesResponseType(typeof(ChunkedUploadCommitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChunkedUploadCommitResponse>> Commit(
        [FromRoute] Guid uploadId,
        [FromBody] CommitChunkedUploadRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Committing block list for chunked upload session {UploadId}.", uploadId);
        var response = await _chunkedUploadService.CommitAsync(uploadId, request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Cancels a chunked upload session and purges uncommitted blocks.
    /// </summary>
    [HttpPost("{uploadId:guid}/cancel")]
    [ProducesResponseType(typeof(CancelUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CancelUploadResponse>> Cancel(
        [FromRoute] Guid uploadId,
        [FromBody] CancelUploadRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling chunked upload session {UploadId}.", uploadId);
        var response = await _chunkedUploadService.CancelAsync(uploadId, request, cancellationToken);
        return Ok(response);
    }
}
