using System.IO;
using System.Text.Json;

namespace TranscribeWin.Models;

public class AppSettings
{
    public string ServerUrl { get; set; } = "http://192.168.80.60:9000";
    public string Language { get; set; } = "zh";
    public int MicrophoneDeviceIndex { get; set; } = 0;
    public string HotkeyModifiers { get; set; } = "Ctrl+Alt";
    public string HotkeyKey { get; set; } = "Space";
    public string RecordMode { get; set; } = "PushToTalk"; // PushToTalk or Toggle
    public bool AutoCopyToClipboard { get; set; } = true;
    public bool AutoEnterOnPunctuation { get; set; } = false;
    public bool Punctuation { get; set; } = true;

    private static readonly string SettingsPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }
}
