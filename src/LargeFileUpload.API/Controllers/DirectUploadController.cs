using LargeFileUpload.Application.Uploads.Dtos;
using LargeFileUpload.Application.Uploads.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LargeFileUpload.API.Controllers;

[ApiController]
[Route("api/uploads/direct")]
[Produces("application/json")]
public class DirectUploadController : BaseApiController
{
    private readonly IDirectUploadService _directUploadService;
    private readonly ILogger<DirectUploadController> _logger;

    public DirectUploadController(
        IDirectUploadService directUploadService,
        ILogger<DirectUploadController> logger)
    {
        _directUploadService = directUploadService;
        _logger = logger;
    }

    /// <summary>
    /// Initiates a direct-to-blob upload session, issuing a short-lived User Delegation SAS URL.
    /// </summary>
    /// <param name="request">File metadata (FileName, FileSize, ContentType).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("initiate")]
    [EnableRateLimiting("UploadInitiatePolicy")]
    [ProducesResponseType(typeof(DirectUploadInitiationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DirectUploadInitiationResponse>> Initiate(
        [FromBody] InitiateDirectUploadRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initiating direct upload for file '{FileName}' ({FileSize} bytes).", request.FileName, request.FileSize);
        var response = await _directUploadService.InitiateAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Completes a direct-to-blob upload session after Azure Blob Storage upload finishes, verifying blob integrity.
    /// </summary>
    /// <param name="uploadId">Unique upload session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{uploadId:guid}/complete")]
    [ProducesResponseType(typeof(CompleteDirectUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CompleteDirectUploadResponse>> Complete(
        [FromRoute] Guid uploadId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Completing direct upload session {UploadId}.", uploadId);
        var response = await _directUploadService.CompleteAsync(uploadId, cancellationToken);
        return Ok(response);
    }
}
