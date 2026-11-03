namespace SoundGuard;

public sealed class FileGuardStore : IGuardStore
{
    private readonly string _path;

    public FileGuardStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SoundGuard", "guarded-device.txt");
    }

    public string? Load()
    {
        try
        {
            var id = File.Exists(_path) ? File.ReadAllText(_path).Trim() : "";
            return id.Length == 0 ? null : id;
        }
        catch (IOException) { return null; }
    }

    public void Save(string endpointId)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, endpointId);
    }
}
