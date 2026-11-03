namespace SoundGuard;

/// <summary>Read-only view of the audio system.</summary>
public interface IAudioQuery
{
    /// <summary>Endpoint id of the current default render device (console role), or null if none.</summary>
    string? GetDefaultRenderId();

    /// <summary>True if the render endpoint exists and is in the Active state.</summary>
    bool IsActive(string endpointId);

    /// <summary>True if the endpoint is one that gets muted when the guarded device is lost (speaker-like).</summary>
    bool IsMuteCandidate(string endpointId);
}

public interface IDefaultDeviceSetter
{
    void SetDefault(string endpointId);
}

public interface ISpeakerMuter
{
    bool IsMuted { get; }

    /// <summary>Mutes all speaker-like endpoints. Safe to call repeatedly; newly appeared endpoints are added.</summary>
    void Mute();

    /// <summary>Restores the mute state captured before <see cref="Mute"/>.</summary>
    void Restore();
}

/// <summary>Persists the id of the guarded device across restarts.</summary>
public interface IGuardStore
{
    string? Load();
    void Save(string endpointId);
}
