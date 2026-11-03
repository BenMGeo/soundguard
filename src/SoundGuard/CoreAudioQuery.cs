using System.Text;
using NAudio.CoreAudioApi;

namespace SoundGuard;

public sealed class CoreAudioQuery : IAudioQuery
{
    private readonly MMDeviceEnumerator _enumerator;
    private readonly Func<Config> _config;

    public CoreAudioQuery(MMDeviceEnumerator enumerator, Func<Config> config)
    {
        _enumerator = enumerator;
        _config = config;
    }

    public string? GetDefaultRenderId()
    {
        try
        {
            using var d = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            return d.ID;
        }
        catch (System.Runtime.InteropServices.COMException) { return null; } // no render device at all
    }

    public bool IsActive(string endpointId)
    {
        try
        {
            using var d = _enumerator.GetDevice(endpointId);
            return d.State == DeviceState.Active;
        }
        catch (Exception) { return false; } // endpoint no longer exists
    }

    public bool IsMuteCandidate(string endpointId)
    {
        try
        {
            using var d = _enumerator.GetDevice(endpointId);
            return DeviceClassifier.ShouldMute(d, _config());
        }
        catch (Exception) { return false; }
    }

    /// <summary>Human-readable list of active render devices, for diagnosing wrong form factors.</summary>
    public string Describe()
    {
        var sb = new StringBuilder();
        var cfg = _config();
        foreach (var d in _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
        {
            using (d)
            {
                var ff = DeviceClassifier.GetFormFactor(d);
                sb.AppendLine($"{d.FriendlyName}  [{DeviceClassifier.FormFactorName(ff)}]  -> {(DeviceClassifier.ShouldMute(d, cfg) ? "muted on loss" : "never muted")}");
            }
        }
        return sb.Length == 0 ? "No active playback devices." : sb.ToString();
    }
}
