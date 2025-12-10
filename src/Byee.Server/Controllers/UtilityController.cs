using Byee.Server.Models;
using Byee.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Byee.Server.Controllers;

[ApiController]
public class UtilityController : ControllerBase
{
    private readonly IScriptTemplateService _scriptService;
    private readonly ILogger<UtilityController> _logger;

    public UtilityController(
        IScriptTemplateService scriptService,
        ILogger<UtilityController> logger)
    {
        _scriptService = scriptService;
        _logger = logger;
    }

    /// <summary>
    /// Get update script - auto-detect platform
    /// </summary>
    [HttpGet("/update")]
    public IActionResult GetUpdateScript()
    {
        var platform = DetectPlatform();
        return GetUpdateScriptForPlatform(platform);
    }

    /// <summary>
    /// Get update script for specific platform
    /// </summary>
    [HttpGet("/update/{platform}")]
    public IActionResult GetUpdateScriptByPlatform(string platform)
    {
        var p = PlatformExtensions.FromString(platform);
        
        if (p == Platform.Unknown)
        {
            return BadRequest(new { error = $"Unknown platform: {platform}. Supported: linux, macos, windows, alpine" });
        }

        return GetUpdateScriptForPlatform(p);
    }

    private IActionResult GetUpdateScriptForPlatform(Platform platform)
    {
        try
        {
            var script = _scriptService.GetUpdateScript(platform);
            var contentType = platform.ToContentType();
            var fileName = platform.ToFileName("byee-update");

            Response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";
            
            return Content(script, contentType);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to get update script for {Platform}", platform);
            return StatusCode(500, new { error = "Update script not available for this platform" });
        }
    }

    /// <summary>
    /// Get uninstall script - auto-detect platform
    /// </summary>
    [HttpGet("/uninstall")]
    public IActionResult GetUninstallScript()
    {
        var platform = DetectPlatform();
        return GetUninstallScriptForPlatform(platform);
    }

    /// <summary>
    /// Get uninstall script for specific platform
    /// </summary>
    [HttpGet("/uninstall/{platform}")]
    public IActionResult GetUninstallScriptByPlatform(string platform)
    {
        var p = PlatformExtensions.FromString(platform);
        
        if (p == Platform.Unknown)
        {
            return BadRequest(new { error = $"Unknown platform: {platform}. Supported: linux, macos, windows, alpine" });
        }

        return GetUninstallScriptForPlatform(p);
    }

    private IActionResult GetUninstallScriptForPlatform(Platform platform)
    {
        try
        {
            var script = _scriptService.GetUninstallScript(platform);
            var contentType = platform.ToContentType();
            var fileName = platform.ToFileName("byee-uninstall");

            Response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";
            
            return Content(script, contentType);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to get uninstall script for {Platform}", platform);
            return StatusCode(500, new { error = "Uninstall script not available for this platform" });
        }
    }

    /// <summary>
    /// Get enable-folders script - auto-detect platform
    /// </summary>
    [HttpGet("/enable-folders")]
    public IActionResult GetEnableFoldersScript()
    {
        var platform = DetectPlatform();
        return GetEnableFoldersScriptForPlatform(platform);
    }

    /// <summary>
    /// Get enable-folders script for specific platform
    /// </summary>
    [HttpGet("/enable-folders/{platform}")]
    public IActionResult GetEnableFoldersScriptByPlatform(string platform)
    {
        var p = PlatformExtensions.FromString(platform);
        
        if (p == Platform.Unknown)
        {
            return BadRequest(new { error = $"Unknown platform: {platform}. Supported: linux, macos, windows, alpine" });
        }

        return GetEnableFoldersScriptForPlatform(p);
    }

    private IActionResult GetEnableFoldersScriptForPlatform(Platform platform)
    {
        try
        {
            var script = _scriptService.GetEnableFoldersScript(platform);
            var contentType = platform.ToContentType();
            var fileName = platform.ToFileName("byee-enable-folders");

            Response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";
            
            return Content(script, contentType);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to get enable-folders script for {Platform}", platform);
            return StatusCode(500, new { error = "Enable-folders script not available for this platform" });
        }
    }

    private Platform DetectPlatform()
    {
        var userAgent = Request.Headers.UserAgent.ToString();
        var platform = PlatformExtensions.FromUserAgent(userAgent);

        _logger.LogDebug("Auto-detected platform {Platform} from User-Agent: {UserAgent}", platform, userAgent);

        if (platform == Platform.Unknown)
            platform = Platform.Linux; // Default to Linux

        return platform;
    }
}
