using NAudio.CoreAudioApi;

namespace SoundGuard;

/// <summary>Mutes speaker-like render endpoints and remembers their prior state.</summary>
public sealed class SpeakerMuter : ISpeakerMuter
{
    private readonly MMDeviceEnumerator _enumerator;
    private readonly Func<Config> _config;
    private readonly Dictionary<string, bool> _original = new();

    public SpeakerMuter(MMDeviceEnumerator enumerator, Func<Config> config)
    {
        _enumerator = enumerator;
        _config = config;
    }

    public bool IsMuted => _original.Count > 0;

    public void Mute()
    {
        var cfg = _config();
        foreach (var d in _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
        {
            using (d)
            {
                if (!DeviceClassifier.ShouldMute(d, cfg)) continue;

                var vol = d.AudioEndpointVolume;
                if (!_original.ContainsKey(d.ID)) _original[d.ID] = vol.Mute;
                if (!vol.Mute) vol.Mute = true;
            }
        }
    }

    public void Restore()
    {
        foreach (var (id, wasMuted) in _original)
        {
            try
            {
                using var d = _enumerator.GetDevice(id);
                d.AudioEndpointVolume.Mute = wasMuted;
            }
            catch (Exception) { /* endpoint gone; nothing to restore */ }
        }
        _original.Clear();
    }
}
