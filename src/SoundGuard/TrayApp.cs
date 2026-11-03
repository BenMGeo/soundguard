using Microsoft.Win32;
using NAudio.CoreAudioApi;

namespace SoundGuard;

public sealed class TrayApp : ApplicationContext
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValue = "SoundGuard";

    private readonly NotifyIcon _icon;
    private readonly MMDeviceEnumerator _enumerator = new();
    private readonly SerialQueue _queue;
    private readonly CoreAudioQuery _query;
    private readonly GuardLogic _logic;
    private readonly AudioSupervisor _supervisor;
    private readonly ToolStripMenuItem _pause = new("Pause");
    private readonly ToolStripMenuItem _autostart = new("Start with Windows");
    private Config _config;
    private bool _paused;

    public TrayApp()
    {
        _config = Config.Load();
        Func<Config> cfg = () => _config;

        _queue = new SerialQueue(ex => Log($"error: {ex.Message}"));
        _query = new CoreAudioQuery(_enumerator, cfg);
        _logic = new GuardLogic(_query, new PolicyConfigDefaultDeviceSetter(), new SpeakerMuter(_enumerator, cfg),
                                new FileGuardStore(), () => _config.RestoreDefaultOnReconnect, Log);
        _supervisor = new AudioSupervisor(_enumerator, _logic, _queue);

        _pause.Click += (_, _) =>
        {
            _paused = !_paused;
            var paused = _paused;
            _queue.Post(() => _logic.SetPaused(paused));
            _pause.Text = paused ? "Resume" : "Pause";
        };
        _autostart.Checked = IsAutostart();
        _autostart.Click += (_, _) => { SetAutostart(!_autostart.Checked); _autostart.Checked = IsAutostart(); };

        var reload = new ToolStripMenuItem("Reload config");
        reload.Click += (_, _) => ReloadConfig();
        var devices = new ToolStripMenuItem("Show devices...");
        devices.Click += (_, _) => MessageBox.Show(_query.Describe(), "SoundGuard - playback devices");
        var exit = new ToolStripMenuItem("Exit");
        exit.Click += (_, _) => ExitThread();

        _icon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Shield,
            Text = "SoundGuard",
            Visible = true,
            ContextMenuStrip = new ContextMenuStrip()
        };
        _icon.ContextMenuStrip.Items.AddRange(new ToolStripItem[] { _pause, reload, devices, _autostart, new ToolStripSeparator(), exit });

        AppDomain.CurrentDomain.ProcessExit += (_, _) => _logic.Shutdown();
        _supervisor.Start();
    }

    private void ReloadConfig()
    {
        try { _config = Config.Load(); _supervisor.Trigger(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "SoundGuard", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    protected override void ExitThreadCore()
    {
        _supervisor.Dispose();
        _queue.Post(_logic.Shutdown);
        _queue.Dispose();
        _icon.Visible = false;
        _icon.Dispose();
        _enumerator.Dispose();
        base.ExitThreadCore();
    }

    private static bool IsAutostart()
    {
        using var k = Registry.CurrentUser.OpenSubKey(RunKey);
        return k?.GetValue(RunValue) != null;
    }

    private static void SetAutostart(bool on)
    {
        using var k = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)!;
        if (on) k.SetValue(RunValue, "\"" + Environment.ProcessPath + "\"");
        else k.DeleteValue(RunValue, throwOnMissingValue: false);
    }

    private static void Log(string msg) => System.Diagnostics.Debug.WriteLine($"[SoundGuard] {msg}");
}
