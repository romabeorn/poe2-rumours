using System.IO;
using System.Reflection;

namespace Rumours.App;

public sealed class AppPaths
{
    public const string DataFolderVariable = "POE2RUMOURS_DATA";

    private const string DefaultRatingsResource = "ratings.json";

    public string DataFolder { get; }
    public string Settings => Path.Combine(DataFolder, "settings.json");
    public string Ratings => Path.Combine(DataFolder, "ratings.json");
    public string DefaultRatings => Path.Combine(DataFolder, "ratings.default.json");
    public string Theme => Path.Combine(DataFolder, "theme");
    public string Logs => Path.Combine(DataFolder, "logs");

    public AppPaths()
    {
        var overridden = Environment.GetEnvironmentVariable(DataFolderVariable);
        DataFolder = string.IsNullOrWhiteSpace(overridden)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PoE2 Rumours")
            : overridden;
        Directory.CreateDirectory(DataFolder);
    }

    public string? CustomRatings()
    {
        TryWrite(DefaultRatings, ShippedRatings());
        return File.Exists(Ratings) ? File.ReadAllText(Ratings) : null;
    }

    public static string ShippedRatings()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(DefaultRatingsResource)
            ?? throw new InvalidOperationException("The default ratings are missing from the build");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static void TryWrite(string path, string content)
    {
        try
        {
            if (!File.Exists(path) || File.ReadAllText(path) != content) File.WriteAllText(path, content);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
        }
    }
}
