using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rumours.App;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions Format = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly string _path;
    private bool _dirty;

    public Settings Current { get; private set; }

    public string? LoadProblem { get; }

    public event Action? Changed;

    public SettingsStore(string path)
    {
        _path = path;
        (Current, LoadProblem) = Load(path);
    }

    public void Update(Func<Settings, Settings> change)
    {
        var updated = change(Current).Sanitised();
        if (updated == Current) return;

        Current = updated;
        _dirty = true;
        Changed?.Invoke();
    }

    public string? Flush()
    {
        if (!_dirty) return null;
        try
        {
            var temporary = _path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(Current, Format));
            File.Move(temporary, _path, overwrite: true);
            _dirty = false;
            return null;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return error.Message;
        }
    }

    private static (Settings, string?) Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return (new Settings(), null);
            var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(path), Format) ?? new Settings();
            return (settings.Sanitised(), null);
        }
        catch (Exception error) when (error is JsonException or IOException or UnauthorizedAccessException)
        {
            return (new Settings(), error.Message);
        }
    }
}
