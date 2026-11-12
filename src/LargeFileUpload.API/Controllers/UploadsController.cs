using LargeFileUpload.Application.Common.Models;
using LargeFileUpload.Application.Uploads.Dtos;
using LargeFileUpload.Application.Uploads.Services;
using Microsoft.AspNetCore.Mvc;

namespace LargeFileUpload.API.Controllers;

[ApiController]
[Route("api/uploads")]
[Produces("application/json")]
public class UploadsController : BaseApiController
{
    private readonly IUploadSessionService _sessionService;
    private readonly ILogger<UploadsController> _logger;

    public UploadsController(
        IUploadSessionService sessionService,
        ILogger<UploadsController> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// Gets details of an upload session by ID.
    /// </summary>
    [HttpGet("{uploadId:guid}")]
    [ProducesResponseType(typeof(UploadSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UploadSessionDto>> GetById(
        [FromRoute] Guid uploadId,
        CancellationToken cancellationToken)
    {
        var session = await _sessionService.GetByIdAsync(uploadId, cancellationToken);
        if (session == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Upload session not found",
                Detail = $"Upload session with ID '{uploadId}' was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(session);
    }

    /// <summary>
    /// Gets paginated history of uploads for the current authenticated user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<UploadSessionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<UploadSessionDto>>> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var history = await _sessionService.GetUserHistoryAsync(page, pageSize, cancellationToken);
        return Ok(history);
    }

    /// <summary>
    /// Cancels or deletes an upload session and purges its blob in Azure Blob Storage.
    /// </summary>
    [HttpDelete("{uploadId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid uploadId,
        CancellationToken cancellationToken)
    {
        var deleted = await _sessionService.DeleteUploadAsync(uploadId, cancellationToken);
        if (!deleted)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Upload session not found",
                Detail = $"Upload session with ID '{uploadId}' was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return NoContent();
    }
}
