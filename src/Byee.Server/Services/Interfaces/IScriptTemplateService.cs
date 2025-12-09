using Byee.Server.Models;

namespace Byee.Server.Services.Interfaces;

/// <summary>
/// Generates platform-specific scripts with instance URL embedded
/// </summary>
public interface IScriptTemplateService
{
    /// <summary>
    /// Get installer script for the specified platform
    /// </summary>
    string GetInstaller(Platform platform);

    /// <summary>
    /// Get client script for the specified platform
    /// </summary>
    string GetClient(Platform platform);
}
