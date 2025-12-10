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

    /// <summary>
    /// Get update script for the specified platform
    /// </summary>
    string GetUpdateScript(Platform platform);

    /// <summary>
    /// Get uninstall script for the specified platform
    /// </summary>
    string GetUninstallScript(Platform platform);

    /// <summary>
    /// Get enable-folders script for the specified platform
    /// </summary>
    string GetEnableFoldersScript(Platform platform);
}
