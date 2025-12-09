using Byee.Server.Configuration;
using Byee.Server.Models;
using Byee.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Byee.Server.Controllers;

[ApiController]
public class UploadController : ControllerBase
{
    private readonly IFileStorageService _storage;
    private readonly ByeeOptions _options;
    private readonly ILogger<UploadController> _logger;

    public UploadController(
        IFileStorageService storage,
        IOptions<ByeeOptions> options,
        ILogger<UploadController> logger)
    {
        _storage = storage;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Upload an encrypted file
    /// Headers:
    ///   X-Byee-Filename: original filename
    ///   X-Byee-Size: original file size (before encryption)
    /// </summary>
    [HttpPost("/upload")]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<IActionResult> Upload(CancellationToken ct)
    {
        // Get metadata from headers
        var fileName = Request.Headers["X-Byee-Filename"].FirstOrDefault() ?? "file";
        var sizeHeader = Request.Headers["X-Byee-Size"].FirstOrDefault();
        
        if (!long.TryParse(sizeHeader, out var originalSize))
        {
            originalSize = 0; // Unknown size
        }

        _logger.LogInformation("Upload started: {FileName} ({Size} bytes)", fileName, originalSize);

        try
        {
            // Stream directly from request body to storage
            var id = await _storage.StoreFileAsync(Request.Body, fileName, originalSize, ct);

            var publicUrl = _options.PublicUrl.TrimEnd('/');
            var command = $"byee receive {id} <KEY>";

            _logger.LogInformation("Upload completed: {Id}", id);

            return Ok(new UploadResult
            {
                Id = id,
                Command = command
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Upload cancelled");
            return StatusCode(499, new { error = "Upload cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload failed");
            return StatusCode(500, new { error = "Upload failed" });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("/health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", version = typeof(UploadController).Assembly.GetName().Version?.ToString() });
    }
}
