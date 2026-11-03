using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace SoundGuard;

/// <summary>Listens to Core Audio notifications and funnels them into <see cref="GuardLogic"/>.</summary>
public sealed class AudioSupervisor : IMMNotificationClient, IDisposable
{
    private readonly MMDeviceEnumerator _enumerator;
    private readonly GuardLogic _logic;
    private readonly SerialQueue _queue;

    public AudioSupervisor(MMDeviceEnumerator enumerator, GuardLogic logic, SerialQueue queue)
    {
        _enumerator = enumerator;
        _logic = logic;
        _queue = queue;
    }

    public void Start()
    {
        _enumerator.RegisterEndpointNotificationCallback(this);
        Trigger();
    }

    public void Trigger() => _queue.Post(_logic.Evaluate);

    public void Dispose() => _enumerator.UnregisterEndpointNotificationCallback(this);

    public void OnDeviceStateChanged(string deviceId, DeviceState newState) => Trigger();
    public void OnDeviceAdded(string pwstrDeviceId) => Trigger();
    public void OnDeviceRemoved(string deviceId) => Trigger();

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        if (flow == DataFlow.Render && role == Role.Console) Trigger();
    }

    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }
}
