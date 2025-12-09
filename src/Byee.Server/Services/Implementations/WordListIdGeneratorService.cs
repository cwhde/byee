using System.Security.Cryptography;
using System.Text;
using Byee.Server.Services.Interfaces;
using Microsoft.Extensions.Options;
using Byee.Server.Configuration;

namespace Byee.Server.Services.Implementations;

public class WordListIdGeneratorService : IIdGeneratorService
{
    private readonly string[] _wordList;
    private readonly ByeeOptions _options;

    public WordListIdGeneratorService(IOptions<ByeeOptions> options)
    {
        _options = options.Value;
        _wordList = LoadWordList();
    }

    private static string[] LoadWordList()
    {
        // Try to load from file first
        var wordListPath = Path.Combine(AppContext.BaseDirectory, "wordlist.txt");
        if (File.Exists(wordListPath))
        {
            return File.ReadAllLines(wordListPath)
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .Select(w => w.Trim().ToLowerInvariant())
                .ToArray();
        }

        // Fallback to embedded word list
        return GetDefaultWordList();
    }

    private static string[] GetDefaultWordList()
    {
        // A curated list of simple, memorable words
        return new[]
        {
            "apple", "banana", "cherry", "dragon", "eagle", "falcon", "garden", "harbor",
            "island", "jungle", "koala", "lemon", "mango", "north", "ocean", "panda",
            "queen", "river", "storm", "tiger", "urban", "violet", "water", "xenon",
            "yellow", "zebra", "anchor", "bridge", "castle", "desert", "forest", "glacier",
            "hollow", "igloo", "jester", "knight", "lantern", "meadow", "nebula", "orange",
            "phoenix", "quartz", "rocket", "silver", "thunder", "umbrella", "valley", "whisper",
            "crystal", "dolphin", "ember", "flame", "golden", "hunter", "ivory", "jasper",
            "karma", "lunar", "marble", "nova", "oasis", "pearl", "quest", "raven",
            "shadow", "temple", "unity", "vortex", "willow", "zephyr", "arctic", "blaze",
            "coral", "dawn", "echo", "frost", "grove", "horizon", "indigo", "jade",
            "kite", "lotus", "mystic", "nectar", "orbit", "prism", "quiet", "rapids",
            "spark", "tidal", "ultra", "venture", "wonder", "zenith", "alpine", "breeze",
            "cipher", "dusk", "electric", "fern", "glow", "haze", "iron", "jewel",
            "kelp", "light", "mirror", "night", "onyx", "plasma", "quantum", "radiant",
            "solar", "torch", "upward", "vivid", "wave", "yarn", "azure", "bronze",
            "carbon", "diamond", "emerald", "fiber", "granite", "helium", "ink", "jet",
            "krypton", "laser", "metal", "neon", "oxide", "pixel", "quasar", "ruby",
            "steel", "titanium", "uranium", "vapor", "wire", "xenial", "yttrium", "zinc",
            "atom", "byte", "comet", "data", "energy", "flux", "gamma", "helix",
            "ion", "joule", "kinetic", "lambda", "matrix", "neutron", "omega", "proton",
            "qubit", "ray", "sigma", "theta", "unit", "vector", "watt", "axis",
            "binary", "core", "delta", "electron", "field", "gravity", "hydro", "isotope",
            "junction", "kernel", "lattice", "momentum", "nucleus", "origin", "particle", "quark"
        };
    }

    public string GenerateId(int wordCount = 4)
    {
        var words = new string[wordCount];
        
        for (int i = 0; i < wordCount; i++)
        {
            var index = RandomNumberGenerator.GetInt32(_wordList.Length);
            words[i] = _wordList[index];
        }

        return string.Join("-", words);
    }

    public bool IsValidId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        var words = id.Split('-');
        
        if (words.Length < 2 || words.Length > 8)
            return false;

        // Each part should be a lowercase alphabetic word
        return words.All(w => !string.IsNullOrEmpty(w) && w.All(char.IsLetter) && w == w.ToLowerInvariant());
    }
}
