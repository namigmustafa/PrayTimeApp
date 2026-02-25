namespace PrayTimeApp.Services;

public static class FileLogger
{
    static readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "notif_log.txt");

    public static void Log(string message)
    {
        try { File.AppendAllText(_path, $"{DateTime.Now:HH:mm:ss.fff}  {message}\n"); }
        catch { }
    }

    public static string Read()
    {
        try { return File.Exists(_path) ? File.ReadAllText(_path) : "(log empty)"; }
        catch (Exception ex) { return $"(read error: {ex.Message})"; }
    }

    public static void Clear()
    {
        try { if (File.Exists(_path)) File.Delete(_path); } catch { }
    }
}
