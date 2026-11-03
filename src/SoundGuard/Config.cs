using System.Text.Json;

namespace SoundGuard;

public sealed class Config
{
    /// <summary>Endpoints whose name contains one of these are always muted while the guarded device is lost.</summary>
    public List<string> MuteDeviceNames { get; set; } = new();

    /// <summary>Endpoints whose name contains one of these are never muted (e.g. a headset reporting itself as Speakers).</summary>
    public List<string> NeverMuteDeviceNames { get; set; } = new();

    public bool RestoreDefaultOnReconnect { get; set; } = true;

    public static string DefaultPath => Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    public static Config Load(string? path = null)
    {
        var json = File.ReadAllText(path ?? DefaultPath);
        return JsonSerializer.Deserialize<Config>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? throw new InvalidDataException("appsettings.json is empty.");
    }
}
