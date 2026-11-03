using NAudio.CoreAudioApi;

namespace SoundGuard;

/// <summary>Decides from the endpoint form factor (and optional name overrides) whether a device is speaker-like.</summary>
public static class DeviceClassifier
{
    // PKEY_AudioEndpoint_FormFactor
    private static readonly PropertyKey FormFactorKey = new(new Guid("1da5d803-d492-4edd-8c23-e0c0ffee7f0e"), 0);

    public const int UnknownFormFactor = 10;

    public static int GetFormFactor(MMDevice device)
    {
        try
        {
            var props = device.Properties;
            if (props.Contains(FormFactorKey)) return Convert.ToInt32(props[FormFactorKey].Value);
        }
        catch (Exception) { /* fall through to unknown */ }
        return UnknownFormFactor;
    }

    public static string FormFactorName(int formFactor) => formFactor switch
    {
        0 => "RemoteNetworkDevice",
        1 => "Speakers",
        2 => "LineLevel",
        3 => "Headphones",
        4 => "Microphone",
        5 => "Headset",
        6 => "Handset",
        7 => "UnknownDigitalPassthrough",
        8 => "SPDIF",
        9 => "DigitalAudioDisplayDevice",
        _ => "Unknown"
    };

    public static bool ShouldMute(MMDevice device, Config cfg)
    {
        var name = device.FriendlyName;
        if (Matches(cfg.NeverMuteDeviceNames, name)) return false;
        if (Matches(cfg.MuteDeviceNames, name)) return true;
        // Speakers, LineLevel, UnknownDigitalPassthrough, SPDIF, DigitalAudioDisplayDevice
        return GetFormFactor(device) is 1 or 2 or 7 or 8 or 9;
    }

    private static bool Matches(List<string> names, string deviceName) =>
        names.Any(n => deviceName.Contains(n, StringComparison.OrdinalIgnoreCase));
}
