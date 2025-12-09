using Byee.Server.Models;

namespace Byee.Server.Services.Interfaces;

/// <summary>
/// Handles encrypted file storage and retrieval
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Store a file stream and return a unique ID
    /// </summary>
    Task<string> StoreFileAsync(Stream fileStream, string fileName, long originalSize, CancellationToken ct = default);

    /// <summary>
    /// Get a file stream for download
    /// </summary>
    Task<Stream?> GetFileStreamAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Get file metadata
    /// </summary>
    Task<FileMetadata?> GetMetadataAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Delete a file
    /// </summary>
    Task DeleteFileAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Check if a file exists
    /// </summary>
    Task<bool> ExistsAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Update file metadata
    /// </summary>
    Task UpdateMetadataAsync(string id, FileMetadata metadata, CancellationToken ct = default);
}
