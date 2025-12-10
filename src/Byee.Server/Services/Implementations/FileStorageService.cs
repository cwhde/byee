using System.Collections.Concurrent;
using System.Text.Json;
using Byee.Server.Configuration;
using Byee.Server.Models;
using Byee.Server.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Byee.Server.Services.Implementations;

public class FileStorageService : IFileStorageService
{
    private readonly ByeeOptions _options;
    private readonly IIdGeneratorService _idGenerator;
    private readonly ILogger<FileStorageService> _logger;
    private readonly string _storagePath;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new();

    public FileStorageService(
        IOptions<ByeeOptions> options,
        IIdGeneratorService idGenerator,
        ILogger<FileStorageService> logger)
    {
        _options = options.Value;
        _idGenerator = idGenerator;
        _logger = logger;
        _storagePath = Path.GetFullPath(_options.StoragePath);

        // Ensure storage directory exists
        Directory.CreateDirectory(_storagePath);
        Directory.CreateDirectory(Path.Combine(_storagePath, "files"));
        Directory.CreateDirectory(Path.Combine(_storagePath, "meta"));
    }

    public async Task<string> StoreFileAsync(Stream fileStream, string fileName, long originalSize, CancellationToken ct = default)
    {
        // Generate unique ID
        string id;
        string filePath;
        do
        {
            id = _idGenerator.GenerateId(_options.IdWordCount);
            filePath = GetFilePath(id);
        } while (File.Exists(filePath));

        var semaphore = _fileLocks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);

        try
        {
            // Write file
            await using var fs = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
            await fileStream.CopyToAsync(fs, 81920, ct);

            var encryptedSize = fs.Length;

            // Write metadata
            var metadata = new FileMetadata
            {
                Id = id,
                FileName = SanitizeFileName(fileName),
                Size = originalSize,
                EncryptedSize = encryptedSize,
                UploadedAt = DateTime.UtcNow,
                Claimed = false
            };

            await WriteMetadataAsync(id, metadata, ct);

            _logger.LogInformation("Stored file {Id} ({FileName}, {Size} bytes)", id, fileName, encryptedSize);

            return id;
        }
        catch
        {
            // Clean up on failure
            if (File.Exists(filePath))
                File.Delete(filePath);
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public Task<Stream?> GetFileStreamAsync(string id, CancellationToken ct = default)
    {
        var filePath = GetFilePath(id);
        
        if (!File.Exists(filePath))
            return Task.FromResult<Stream?>(null);

        return Task.FromResult<Stream?>(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan));
    }

    public async Task<FileMetadata?> GetMetadataAsync(string id, CancellationToken ct = default)
    {
        var metaPath = GetMetaPath(id);
        
        if (!File.Exists(metaPath))
            return null;

        var json = await File.ReadAllTextAsync(metaPath, ct);
        return JsonSerializer.Deserialize<FileMetadata>(json);
    }

    public async Task DeleteFileAsync(string id, CancellationToken ct = default)
    {
        var semaphore = _fileLocks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);

        try
        {
            var filePath = GetFilePath(id);
            var metaPath = GetMetaPath(id);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted file {Id}", id);
            }

            if (File.Exists(metaPath))
                File.Delete(metaPath);

            _fileLocks.TryRemove(id, out _);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public Task<bool> ExistsAsync(string id, CancellationToken ct = default)
    {
        var filePath = GetFilePath(id);
        return Task.FromResult(File.Exists(filePath));
    }

    public async Task UpdateMetadataAsync(string id, FileMetadata metadata, CancellationToken ct = default)
    {
        var semaphore = _fileLocks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);

        try
        {
            await WriteMetadataAsync(id, metadata, ct);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task WriteMetadataAsync(string id, FileMetadata metadata, CancellationToken ct)
    {
        var metaPath = GetMetaPath(id);
        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(metaPath, json, ct);
    }

    private string GetFilePath(string id) => Path.Combine(_storagePath, "files", id);
    private string GetMetaPath(string id) => Path.Combine(_storagePath, "meta", $"{id}.json");

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "file";

        // Remove path components
        fileName = Path.GetFileName(fileName);

        // Remove invalid characters
        var invalid = Path.GetInvalidFileNameChars();
        fileName = new string(fileName.Where(c => !invalid.Contains(c)).ToArray());

        // Limit length
        if (fileName.Length > 255)
            fileName = fileName[..255];

        return string.IsNullOrWhiteSpace(fileName) ? "file" : fileName;
    }
}
