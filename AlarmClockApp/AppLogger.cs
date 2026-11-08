namespace AlarmClockApp;

internal static class AppLogger
{
    private static readonly string AppDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AlarmClockApp");

    internal static string LogPath => Path.Combine(AppDirectory, "app.log");

    internal static void Log(Exception exception)
    {
        try
        {
            Directory.CreateDirectory(AppDirectory);
            File.AppendAllText(
                LogPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (Exception logException)
        {
            System.Diagnostics.Debug.WriteLine($"Unable to write the application log: {logException}");
            System.Diagnostics.Debug.WriteLine(exception);
        }
    }
}
