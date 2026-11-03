namespace SoundGuard;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, @"Local\SoundGuard.SingleInstance", out var isFirst);
        if (!isFirst) return;

        ApplicationConfiguration.Initialize();
        try { Application.Run(new TrayApp()); }
        catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException)
        {
            MessageBox.Show(ex.Message, "SoundGuard", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
