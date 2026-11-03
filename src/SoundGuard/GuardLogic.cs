namespace SoundGuard;

/// <summary>
/// Tracks the "guarded" device (the one the user last had as default). When it disappears and
/// Windows falls back to a speaker-like device, speakers are muted until the guarded device returns.
/// Every audio event just triggers <see cref="Evaluate"/>, which is idempotent.
/// Not thread-safe; callers must serialize (see <see cref="SerialQueue"/>).
/// </summary>
public sealed class GuardLogic
{
    private readonly IAudioQuery _audio;
    private readonly IDefaultDeviceSetter _setter;
    private readonly ISpeakerMuter _muter;
    private readonly IGuardStore _store;
    private readonly Func<bool> _restoreDefault;
    private readonly Action<string> _log;

    private string? _guarded;
    private bool _loaded;

    public bool Paused { get; private set; }

    public GuardLogic(IAudioQuery audio, IDefaultDeviceSetter setter, ISpeakerMuter muter, IGuardStore store,
                      Func<bool> restoreDefault, Action<string>? log = null)
    {
        _audio = audio;
        _setter = setter;
        _muter = muter;
        _store = store;
        _restoreDefault = restoreDefault;
        _log = log ?? (_ => { });
    }

    public void SetPaused(bool paused)
    {
        Paused = paused;
        Evaluate();
    }

    public void Evaluate()
    {
        if (!_loaded)
        {
            _guarded = _store.Load();
            _loaded = true;
        }

        if (Paused)
        {
            if (_muter.IsMuted) _muter.Restore();
            return;
        }

        var def = _audio.GetDefaultRenderId();

        if (_guarded == null)
        {
            Adopt(def);
            return;
        }

        if (def == _guarded)
        {
            if (_muter.IsMuted) _muter.Restore();
            return;
        }

        if (_audio.IsActive(_guarded))
        {
            if (_muter.IsMuted)
            {
                // Guarded device came back after a loss.
                _muter.Restore();
                if (_restoreDefault())
                {
                    TrySetDefault(_guarded);
                    return;
                }
            }
            Adopt(def); // deliberate switch by the user
            return;
        }

        // Guarded device is lost. Another headset-type fallback is fine; speakers are not.
        if (def != null && !_audio.IsMuteCandidate(def))
        {
            if (_muter.IsMuted) _muter.Restore();
            Adopt(def);
            return;
        }

        _muter.Mute();
    }

    public void Shutdown()
    {
        if (_muter.IsMuted) _muter.Restore();
    }

    private void Adopt(string? endpointId)
    {
        if (endpointId == null || endpointId == _guarded) return;
        _guarded = endpointId;
        try { _store.Save(endpointId); }
        catch (Exception ex) { _log($"Saving state failed: {ex.Message}"); }
    }

    private void TrySetDefault(string endpointId)
    {
        try { _setter.SetDefault(endpointId); }
        catch (Exception ex) { _log($"SetDefault failed: {ex.Message}"); }
    }
}
