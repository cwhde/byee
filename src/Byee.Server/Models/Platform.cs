namespace Byee.Server.Models;

public enum Platform
{
    Unknown,
    Linux,
    MacOS,
    Windows,
    Alpine
}

public static class PlatformExtensions
{
    public static Platform FromString(string? platform)
    {
        return platform?.ToLowerInvariant() switch
        {
            "linux" => Platform.Linux,
            "macos" or "mac" or "darwin" or "osx" => Platform.MacOS,
            "windows" or "win" or "win32" or "win64" => Platform.Windows,
            "alpine" => Platform.Alpine,
            _ => Platform.Unknown
        };
    }

    public static Platform FromUserAgent(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return Platform.Unknown;

        var ua = userAgent.ToLowerInvariant();

        // Check for Windows first
        if (ua.Contains("windows") || ua.Contains("win32") || ua.Contains("win64"))
            return Platform.Windows;

        // Check for macOS
        if (ua.Contains("macintosh") || ua.Contains("mac os") || ua.Contains("darwin"))
            return Platform.MacOS;

        // Check for Alpine (usually in container user agents)
        if (ua.Contains("alpine"))
            return Platform.Alpine;

        // Check for Linux
        if (ua.Contains("linux") || ua.Contains("x11"))
            return Platform.Linux;

        // curl default (no user agent or just "curl/x.x.x") - assume Linux
        if (ua.StartsWith("curl/") || ua == "curl")
            return Platform.Linux;

        // wget
        if (ua.StartsWith("wget/") || ua == "wget")
            return Platform.Linux;

        // PowerShell
        if (ua.Contains("powershell") || ua.Contains("windowspowershell"))
            return Platform.Windows;

        return Platform.Unknown;
    }

    public static string ToScriptExtension(this Platform platform)
    {
        return platform switch
        {
            Platform.Windows => "ps1",
            _ => "sh"
        };
    }

    public static string ToContentType(this Platform platform)
    {
        return platform switch
        {
            Platform.Windows => "text/plain",
            _ => "text/plain"
        };
    }

    public static string ToFileName(this Platform platform, string baseName)
    {
        return platform switch
        {
            Platform.Windows => $"{baseName}.ps1",
            _ => $"{baseName}.sh"
        };
    }
}
