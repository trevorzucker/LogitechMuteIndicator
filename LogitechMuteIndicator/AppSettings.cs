using System.Text.Json;
namespace LogitechMuteIndicator;

public class AppSettings
{
    public string? SelectedDeviceId { get; set; }
}


public static class SettingsLoader {
    const string SETTINGS_FILE = "settings.json";
    static readonly AppSettings defaultSettings = new() {
        SelectedDeviceId = Program.DEFAULT_AUDIO_DEVICE_ID
    };
    

    public static AppSettings LoadSettings()
    {
        if (!File.Exists(SETTINGS_FILE))
            return defaultSettings;

        try
        {
            string json = File.ReadAllText(SETTINGS_FILE);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? defaultSettings;
        }
        catch
        {
            return defaultSettings;
        }
    }

    public static void SaveSettings(AppSettings settings)
    {
        string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SETTINGS_FILE, json);
    }
}