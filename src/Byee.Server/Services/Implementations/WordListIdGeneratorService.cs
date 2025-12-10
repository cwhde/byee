using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Byee.Server.Services.Interfaces;

namespace Byee.Server.Services.Implementations;

public class WordListIdGeneratorService : IIdGeneratorService
{
    private static readonly Lazy<string[]> _lazyWordList = new(LoadWordList);
    private static string[] WordList => _lazyWordList.Value;

    // ID format: word + number (10-999), e.g., "porcupine42"
    private static readonly Regex IdPattern = new(@"^[a-z]+\d{2,3}$", RegexOptions.Compiled);

    private static string[] LoadWordList()
    {
        // Try to load from file first
        var wordListPath = Path.Combine(AppContext.BaseDirectory, "wordlist.txt");
        if (File.Exists(wordListPath))
        {
            var words = File.ReadAllLines(wordListPath)
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .Select(w => w.Trim().ToLowerInvariant())
                .Distinct()
                .ToArray();
            
            if (words.Length >= 100)
                return words;
        }

        // Fallback to a minimal embedded list (should not happen in production)
        return GetDefaultWordList();
    }

    private static string[] GetDefaultWordList()
    {
        // Minimal fallback list - production should use wordlist.txt (4096 words)
        return new[]
        {
            "apple", "banana", "cherry", "dragon", "eagle", "falcon", "garden", "harbor",
            "island", "jungle", "koala", "lemon", "mango", "north", "ocean", "panda",
            "queen", "river", "storm", "tiger", "violet", "water", "yellow", "zebra",
            "anchor", "bridge", "castle", "desert", "forest", "meadow", "phoenix", "rocket"
        };
    }

    /// <summary>
    /// Generate a file ID in format: word + number (10-999)
    /// Example: "porcupine42", "ablaze917"
    /// </summary>
    public string GenerateId(int wordCount = 1)
    {
        // wordCount parameter is kept for interface compatibility but ignored
        // ID is always: single word + number
        var wordIndex = RandomNumberGenerator.GetInt32(WordList.Length);
        var number = RandomNumberGenerator.GetInt32(10, 1000); // 10-999
        
        return $"{WordList[wordIndex]}{number}";
    }

    /// <summary>
    /// Validate ID format: lowercase word followed by 2-3 digits
    /// </summary>
    public bool IsValidId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        return IdPattern.IsMatch(id);
    }

    /// <summary>
    /// Generate a passphrase for encryption: 4 random words separated by hyphens
    /// Example: "ablaze-dolphin-crystal-meadow"
    /// </summary>
    public static string GeneratePassphrase(int wordCount = 4)
    {
        var words = new string[wordCount];
        
        for (int i = 0; i < wordCount; i++)
        {
            var index = RandomNumberGenerator.GetInt32(WordList.Length);
            words[i] = WordList[index];
        }

        return string.Join("-", words);
    }

    /// <summary>
    /// Validate passphrase format: 2-8 lowercase words separated by hyphens
    /// </summary>
    public static bool IsValidPassphrase(string passphrase)
    {
        if (string.IsNullOrWhiteSpace(passphrase))
            return false;

        var words = passphrase.Split('-');
        
        if (words.Length < 2 || words.Length > 8)
            return false;

        return words.All(w => !string.IsNullOrEmpty(w) && w.All(char.IsLetter) && w == w.ToLowerInvariant());
    }
}
