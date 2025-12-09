using Byee.Server.Models;
using Byee.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Byee.Server.Controllers;

[ApiController]
public class InstallController : ControllerBase
{
    private readonly IScriptTemplateService _scriptService;
    private readonly ILogger<InstallController> _logger;

    public InstallController(
        IScriptTemplateService scriptService,
        ILogger<InstallController> logger)
    {
        _scriptService = scriptService;
        _logger = logger;
    }

    /// <summary>
    /// Auto-detect platform and return installer script
    /// </summary>
    [HttpGet("/")]
    public IActionResult GetInstaller()
    {
        var userAgent = Request.Headers.UserAgent.ToString();
        var platform = PlatformExtensions.FromUserAgent(userAgent);

        _logger.LogInformation("Auto-detected platform {Platform} from User-Agent: {UserAgent}", platform, userAgent);

        if (platform == Platform.Unknown)
            platform = Platform.Linux; // Default to Linux/bash

        return GetInstallerForPlatform(platform);
    }

    /// <summary>
    /// Get installer for specific platform
    /// </summary>
    [HttpGet("/install/{platform}")]
    public IActionResult GetInstallerByPlatform(string platform)
    {
        var p = PlatformExtensions.FromString(platform);
        
        if (p == Platform.Unknown)
        {
            return BadRequest(new { error = $"Unknown platform: {platform}. Supported: linux, macos, windows, alpine" });
        }

        return GetInstallerForPlatform(p);
    }

    private IActionResult GetInstallerForPlatform(Platform platform)
    {
        try
        {
            var script = _scriptService.GetInstaller(platform);
            var contentType = platform.ToContentType();
            var fileName = platform.ToFileName("byee-install");

            Response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";
            
            return Content(script, contentType);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to get installer for {Platform}", platform);
            return StatusCode(500, new { error = "Installer not available for this platform" });
        }
    }
}
