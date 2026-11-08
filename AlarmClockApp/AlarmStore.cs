using System.Text.Json;
using System.Text.Json.Serialization;

namespace AlarmClockApp;

internal static class AlarmStore
{
    private static readonly string AppDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AlarmClockApp");

    private static readonly string SettingsPath = Path.Combine(AppDirectory, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    internal static AlarmConfiguration Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return new AlarmConfiguration();
        }

        string json = File.ReadAllText(SettingsPath);
        return JsonSerializer.Deserialize<AlarmConfiguration>(json, JsonOptions)
            ?? throw new InvalidDataException("配置文件内容为空或格式无效。");
    }

    internal static void Save(AlarmConfiguration configuration)
    {
        Directory.CreateDirectory(AppDirectory);
        string temporaryPath = $"{SettingsPath}.tmp";
        string json = JsonSerializer.Serialize(configuration, JsonOptions);

        try
        {
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, SettingsPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
