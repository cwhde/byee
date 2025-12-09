using Byee.Server.Models;

namespace Byee.Server.Services.Interfaces;

/// <summary>
/// Handles atomic file claiming for one-time downloads
/// </summary>
public interface IFileClaimService
{
    /// <summary>
    /// Try to claim a file for download. Returns claim token if successful, null if already claimed.
    /// Once claimed, file becomes unavailable to other clients.
    /// </summary>
    Task<string?> TryClaimFileAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Validate a claim token for a file
    /// </summary>
    Task<bool> ValidateClaimAsync(string id, string claimToken, CancellationToken ct = default);

    /// <summary>
    /// Mark download as completed and delete the file
    /// </summary>
    Task CompleteDownloadAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Clean up stale claimed files that were never downloaded
    /// </summary>
    Task CleanupStaleClaimsAsync(CancellationToken ct = default);
}
