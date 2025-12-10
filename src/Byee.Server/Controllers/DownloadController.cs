using Byee.Server.Models;
using Byee.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Byee.Server.Controllers;

[ApiController]
public class DownloadController : ControllerBase
{
    private readonly IFileStorageService _storage;
    private readonly IFileClaimService _claimService;
    private readonly ILogger<DownloadController> _logger;

    public DownloadController(
        IFileStorageService storage,
        IFileClaimService claimService,
        ILogger<DownloadController> logger)
    {
        _storage = storage;
        _claimService = claimService;
        _logger = logger;
    }

    /// <summary>
    /// Get file info and claim the file (making it unavailable to others)
    /// Query params:
    ///   ?info=true - Get metadata and claim the file, returns JSON with claim token
    /// Headers (for actual download):
    ///   X-Byee-Claim-Token: claim token from info request
    /// </summary>
    [HttpGet("/download/{id}")]
    public async Task<IActionResult> Download(string id, [FromQuery] bool info = false, CancellationToken ct = default)
    {
        // Validate ID format
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new { error = "Invalid file ID" });
        }

        // Check if file exists
        if (!await _storage.ExistsAsync(id, ct))
        {
            return NotFound(new { error = "File not found or already downloaded" });
        }

        // Info request - claim the file and return metadata
        if (info)
        {
            return await HandleInfoRequest(id, ct);
        }

        // Download request - need valid claim token
        return await HandleDownloadRequest(id, ct);
    }

    private async Task<IActionResult> HandleInfoRequest(string id, CancellationToken ct)
    {
        var metadata = await _storage.GetMetadataAsync(id, ct);
        
        if (metadata == null)
        {
            return NotFound(new { error = "File not found" });
        }

        // Try to claim the file
        var claimToken = await _claimService.TryClaimFileAsync(id, ct);
        
        if (claimToken == null)
        {
            return Conflict(new { error = "File already claimed by another client" });
        }

        _logger.LogInformation("File {Id} info requested and claimed", id);

        return Ok(new FileInfoResponse
        {
            FileName = metadata.FileName,
            Size = metadata.Size,
            SizeHuman = FormatSize(metadata.Size),
            ClaimToken = claimToken,
            IsFolder = metadata.IsFolder
        });
    }

    private async Task<IActionResult> HandleDownloadRequest(string id, CancellationToken ct)
    {
        // Get claim token from header
        var claimToken = Request.Headers["X-Byee-Claim-Token"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(claimToken))
        {
            return BadRequest(new { error = "Missing claim token. Request with ?info=true first to claim the file." });
        }

        // Validate claim
        if (!await _claimService.ValidateClaimAsync(id, claimToken, ct))
        {
            return Unauthorized(new { error = "Invalid or expired claim token" });
        }

        var metadata = await _storage.GetMetadataAsync(id, ct);
        if (metadata == null)
        {
            return NotFound(new { error = "File not found" });
        }

        // Get file stream
        var stream = await _storage.GetFileStreamAsync(id, ct);
        if (stream == null)
        {
            return NotFound(new { error = "File data not found" });
        }

        _logger.LogInformation("Download started: {Id} ({FileName})", id, metadata.FileName);

        // Set headers
        Response.Headers.ContentDisposition = $"attachment; filename=\"{metadata.FileName}.age\"";
        Response.Headers["X-Byee-Filename"] = metadata.FileName;
        Response.Headers["X-Byee-Size"] = metadata.Size.ToString();
        Response.Headers["X-Byee-IsFolder"] = metadata.IsFolder.ToString().ToLowerInvariant();
        Response.ContentLength = metadata.EncryptedSize;

        // Register callback to delete file after download completes
        Response.OnCompleted(async () =>
        {
            _logger.LogInformation("Download completed: {Id}, deleting file", id);
            await _claimService.CompleteDownloadAsync(id, CancellationToken.None);
        });

        return File(stream, "application/octet-stream", enableRangeProcessing: false);
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}
