using System.Security.Cryptography;
using Byee.Server.Configuration;
using Byee.Server.Models;
using Byee.Server.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Byee.Server.Services.Implementations;

public class FileClaimService : IFileClaimService
{
    private readonly IFileStorageService _storage;
    private readonly ByeeOptions _options;
    private readonly ILogger<FileClaimService> _logger;

    public FileClaimService(
        IFileStorageService storage,
        IOptions<ByeeOptions> options,
        ILogger<FileClaimService> logger)
    {
        _storage = storage;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string?> TryClaimFileAsync(string id, CancellationToken ct = default)
    {
        var metadata = await _storage.GetMetadataAsync(id, ct);
        
        if (metadata == null)
        {
            _logger.LogWarning("Claim attempt for non-existent file {Id}", id);
            return null;
        }

        if (metadata.Claimed)
        {
            _logger.LogWarning("Claim attempt for already claimed file {Id}", id);
            return null;
        }

        // Generate claim token
        var claimToken = GenerateClaimToken();

        // Mark as claimed
        metadata.Claimed = true;
        metadata.ClaimedAt = DateTime.UtcNow;
        metadata.ClaimToken = claimToken;

        await _storage.UpdateMetadataAsync(id, metadata, ct);

        _logger.LogInformation("File {Id} claimed", id);

        return claimToken;
    }

    public async Task<bool> ValidateClaimAsync(string id, string claimToken, CancellationToken ct = default)
    {
        var metadata = await _storage.GetMetadataAsync(id, ct);
        
        if (metadata == null)
            return false;

        return metadata.Claimed && metadata.ClaimToken == claimToken;
    }

    public async Task CompleteDownloadAsync(string id, CancellationToken ct = default)
    {
        _logger.LogInformation("Download completed for {Id}, deleting file", id);
        await _storage.DeleteFileAsync(id, ct);
    }

    public async Task CleanupStaleClaimsAsync(CancellationToken ct = default)
    {
        var storagePath = Path.GetFullPath(_options.StoragePath);
        var metaPath = Path.Combine(storagePath, "meta");

        if (!Directory.Exists(metaPath))
            return;

        var now = DateTime.UtcNow;
        var metaFiles = Directory.GetFiles(metaPath, "*.json");

        foreach (var file in metaFiles)
        {
            try
            {
                var id = Path.GetFileNameWithoutExtension(file);
                var metadata = await _storage.GetMetadataAsync(id, ct);

                if (metadata == null)
                    continue;

                // Delete unclaimed files older than timeout
                if (!metadata.Claimed && (now - metadata.UploadedAt) > _options.ClaimedFileTimeout)
                {
                    _logger.LogInformation("Cleaning up stale unclaimed file {Id}", id);
                    await _storage.DeleteFileAsync(id, ct);
                    continue;
                }

                // Delete claimed files that were never downloaded (claim timeout)
                if (metadata.Claimed && metadata.ClaimedAt.HasValue &&
                    (now - metadata.ClaimedAt.Value) > _options.ClaimedFileTimeout)
                {
                    _logger.LogInformation("Cleaning up stale claimed file {Id}", id);
                    await _storage.DeleteFileAsync(id, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up file {File}", file);
            }
        }
    }

    private static string GenerateClaimToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
