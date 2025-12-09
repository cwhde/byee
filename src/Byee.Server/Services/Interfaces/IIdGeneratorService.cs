namespace Byee.Server.Services.Interfaces;

/// <summary>
/// Generates human-readable word-based IDs
/// </summary>
public interface IIdGeneratorService
{
    /// <summary>
    /// Generate a new unique ID from random words
    /// </summary>
    string GenerateId(int wordCount = 4);

    /// <summary>
    /// Validate that an ID follows the expected format
    /// </summary>
    bool IsValidId(string id);
}
