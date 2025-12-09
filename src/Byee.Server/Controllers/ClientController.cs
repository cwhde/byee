using Byee.Server.Models;
using Byee.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Byee.Server.Controllers;

[ApiController]
public class ClientController : ControllerBase
{
    private readonly IScriptTemplateService _scriptService;
    private readonly ILogger<ClientController> _logger;

    public ClientController(
        IScriptTemplateService scriptService,
        ILogger<ClientController> logger)
    {
        _scriptService = scriptService;
        _logger = logger;
    }

    /// <summary>
    /// Get client script for specific platform
    /// </summary>
    [HttpGet("/client/{platform}")]
    public IActionResult GetClient(string platform)
    {
        var p = PlatformExtensions.FromString(platform);
        
        if (p == Platform.Unknown)
        {
            return BadRequest(new { error = $"Unknown platform: {platform}. Supported: linux, macos, windows, alpine" });
        }

        try
        {
            var script = _scriptService.GetClient(p);
            var contentType = p.ToContentType();
            var fileName = p.ToFileName("byee");

            Response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";
            
            return Content(script, contentType);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to get client for {Platform}", platform);
            return StatusCode(500, new { error = "Client not available for this platform" });
        }
    }

    /// <summary>
    /// Auto-detect platform and return client script
    /// </summary>
    [HttpGet("/client")]
    public IActionResult GetClientAutoDetect()
    {
        var userAgent = Request.Headers.UserAgent.ToString();
        var platform = PlatformExtensions.FromUserAgent(userAgent);

        if (platform == Platform.Unknown)
            platform = Platform.Linux;

        return GetClient(platform.ToString().ToLowerInvariant());
    }
}
