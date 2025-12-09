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

        foreach (var resourceName in resourceNames)
        {
            if (!resourceName.Contains(".Scripts."))
                continue;

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) continue;

            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();

            // Parse resource name to determine type and platform
            // Format: Byee.Server.Scripts.Installers.install.sh.template
            if (resourceName.Contains(".Installers."))
            {
                if (resourceName.EndsWith(".sh.template"))
                {
                    _templateCache[("installer", Platform.Linux)] = content;
                    _templateCache[("installer", Platform.MacOS)] = content;
                    _templateCache[("installer", Platform.Alpine)] = content;
                }
                else if (resourceName.EndsWith(".ps1.template"))
                {
                    _templateCache[("installer", Platform.Windows)] = content;
                }
            }
            else if (resourceName.Contains(".Clients."))
            {
                if (resourceName.EndsWith(".sh.template"))
                {
                    _templateCache[("client", Platform.Linux)] = content;
                    _templateCache[("client", Platform.MacOS)] = content;
                    _templateCache[("client", Platform.Alpine)] = content;
                }
                else if (resourceName.EndsWith(".ps1.template"))
                {
                    _templateCache[("client", Platform.Windows)] = content;
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

    private string ProcessTemplate(string template)
    {
        var publicUrl = _options.PublicUrl.TrimEnd('/');
        
        return template
            .Replace("{{BYEE_URL}}", publicUrl)
            .Replace("{{BYEE_VERSION}}", GetVersion());
    }

    private static string GetVersion()
    {
        return Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "1.0.0";
    }
}
