using System.Reflection;
using Byee.Server.Configuration;
using Byee.Server.Models;
using Byee.Server.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Byee.Server.Services.Implementations;

public class ScriptTemplateService : IScriptTemplateService
{
    private readonly ByeeOptions _options;
    private readonly ILogger<ScriptTemplateService> _logger;
    private readonly Dictionary<(string Type, Platform Platform), string> _templateCache = new();

    public ScriptTemplateService(
        IOptions<ByeeOptions> options,
        ILogger<ScriptTemplateService> logger)
    {
        _options = options.Value;
        _logger = logger;
        LoadTemplates();
    }

    private void LoadTemplates()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();
        
        _logger.LogDebug("Found {Count} embedded resources: {Names}", 
            resourceNames.Length, string.Join(", ", resourceNames));

        foreach (var resourceName in resourceNames)
        {
            // Check for Scripts in resource name (case-insensitive for cross-platform)
            if (!resourceName.Contains("Scripts", StringComparison.OrdinalIgnoreCase))
                continue;

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) continue;

            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();

            // Parse resource name to determine type and platform
            // Format: Byee.Server.Scripts.Installers.install.sh.template
            var isInstaller = resourceName.Contains("Installers", StringComparison.OrdinalIgnoreCase);
            var isClient = resourceName.Contains("Clients", StringComparison.OrdinalIgnoreCase);
            var isUtility = resourceName.Contains("Utilities", StringComparison.OrdinalIgnoreCase);
            var isShell = resourceName.EndsWith(".sh.template", StringComparison.OrdinalIgnoreCase);
            var isPowerShell = resourceName.EndsWith(".ps1.template", StringComparison.OrdinalIgnoreCase);
            
            _logger.LogDebug("Processing resource {Name}: installer={IsInstaller}, client={IsClient}, utility={IsUtility}, sh={IsSh}, ps1={IsPs1}",
                resourceName, isInstaller, isClient, isUtility, isShell, isPowerShell);

            if (isInstaller)
            {
                if (isShell)
                {
                    _templateCache[("installer", Platform.Linux)] = content;
                    _templateCache[("installer", Platform.MacOS)] = content;
                    _templateCache[("installer", Platform.Alpine)] = content;
                    _logger.LogDebug("Loaded shell installer template for Linux/MacOS/Alpine");
                }
                else if (isPowerShell)
                {
                    _templateCache[("installer", Platform.Windows)] = content;
                    _logger.LogDebug("Loaded PowerShell installer template for Windows");
                }
            }
            else if (isClient)
            {
                if (isShell)
                {
                    _templateCache[("client", Platform.Linux)] = content;
                    _templateCache[("client", Platform.MacOS)] = content;
                    _templateCache[("client", Platform.Alpine)] = content;
                    _logger.LogDebug("Loaded shell client template for Linux/MacOS/Alpine");
                }
                else if (isPowerShell)
                {
                    _templateCache[("client", Platform.Windows)] = content;
                    _logger.LogDebug("Loaded PowerShell client template for Windows");
                }
            }
            else if (isUtility)
            {
                // Determine which utility script this is
                var scriptType = resourceName.ToLowerInvariant() switch
                {
                    var n when n.Contains("update") => "update",
                    var n when n.Contains("uninstall") => "uninstall",
                    var n when n.Contains("enable-folders") => "enable-folders",
                    _ => null
                };

                if (scriptType != null)
                {
                    if (isShell)
                    {
                        _templateCache[(scriptType, Platform.Linux)] = content;
                        _templateCache[(scriptType, Platform.MacOS)] = content;
                        _templateCache[(scriptType, Platform.Alpine)] = content;
                        _logger.LogDebug("Loaded shell {ScriptType} template for Linux/MacOS/Alpine", scriptType);
                    }
                    else if (isPowerShell)
                    {
                        _templateCache[(scriptType, Platform.Windows)] = content;
                        _logger.LogDebug("Loaded PowerShell {ScriptType} template for Windows", scriptType);
                    }
                }
            }
        }

        _logger.LogInformation("Loaded {Count} script templates", _templateCache.Count);
    }

    public string GetInstaller(Platform platform)
    {
        if (platform == Platform.Unknown)
            platform = Platform.Linux; // Default to Linux

        if (!_templateCache.TryGetValue(("installer", platform), out var template))
        {
            _logger.LogWarning("No installer template found for platform {Platform}", platform);
            throw new InvalidOperationException($"No installer template for platform {platform}");
        }

        return ProcessTemplate(template);
    }

    public string GetClient(Platform platform)
    {
        if (platform == Platform.Unknown)
            platform = Platform.Linux;

        if (!_templateCache.TryGetValue(("client", platform), out var template))
        {
            _logger.LogWarning("No client template found for platform {Platform}", platform);
            throw new InvalidOperationException($"No client template for platform {platform}");
        }

        return ProcessTemplate(template);
    }

    public string GetUpdateScript(Platform platform)
    {
        if (platform == Platform.Unknown)
            platform = Platform.Linux;

        if (!_templateCache.TryGetValue(("update", platform), out var template))
        {
            _logger.LogWarning("No update script template found for platform {Platform}", platform);
            throw new InvalidOperationException($"No update script template for platform {platform}");
        }

        return ProcessTemplate(template);
    }

    public string GetUninstallScript(Platform platform)
    {
        if (platform == Platform.Unknown)
            platform = Platform.Linux;

        if (!_templateCache.TryGetValue(("uninstall", platform), out var template))
        {
            _logger.LogWarning("No uninstall script template found for platform {Platform}", platform);
            throw new InvalidOperationException($"No uninstall script template for platform {platform}");
        }

        return ProcessTemplate(template);
    }

    public string GetEnableFoldersScript(Platform platform)
    {
        if (platform == Platform.Unknown)
            platform = Platform.Linux;

        if (!_templateCache.TryGetValue(("enable-folders", platform), out var template))
        {
            _logger.LogWarning("No enable-folders script template found for platform {Platform}", platform);
            throw new InvalidOperationException($"No enable-folders script template for platform {platform}");
        }

        return ProcessTemplate(template);
    }

    private string ProcessTemplate(string template)
    {
        var publicUrl = _options.PublicUrl.TrimEnd('/');
        var wordList = GetWordListForClient();
        
        return template
            .Replace("{{BYEE_URL}}", publicUrl)
            .Replace("{{BYEE_VERSION}}", GetVersion())
            .Replace("{{BYEE_WORDLIST}}", wordList);
    }

    private static string GetWordListForClient()
    {
        // Load wordlist and return as comma-separated string for shell scripts
        var wordListPath = Path.Combine(AppContext.BaseDirectory, "wordlist.txt");
        if (File.Exists(wordListPath))
        {
            var words = File.ReadAllLines(wordListPath)
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .Select(w => w.Trim().ToLowerInvariant())
                .Distinct();
            return string.Join(",", words);
        }
        
        // Fallback minimal list
        return "apple,banana,cherry,dragon,eagle,falcon,garden,harbor,island,jungle,koala,lemon,mango,north,ocean,panda,queen,river,storm,tiger,violet,water,yellow,zebra,anchor,bridge,castle,desert,forest,meadow,phoenix,rocket";
    }

    private static string GetVersion()
    {
        return Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "1.0.0";
    }
}
