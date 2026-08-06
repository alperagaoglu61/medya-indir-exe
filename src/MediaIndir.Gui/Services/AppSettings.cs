using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediaIndir.Core;

namespace MediaIndir.Gui.Services;

/// <summary>Kullanici ayarlari. %APPDATA%\MediaIndir\settings.json dosyasinda tutulur.</summary>
public sealed class AppSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MediaIndir", "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string OutputFolder { get; set; } = DownloadPaths.DefaultRoot;

    /// <summary>Pano izleme acik mi?</summary>
    public bool ClipboardWatch { get; set; } = true;

    /// <summary>Son secilen format ("mp4" / "mp3").</summary>
    public string Format { get; set; } = "mp4";

    public string Mp4Quality { get; set; } = "1080";

    public string Mp3Quality { get; set; } = "320";

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (loaded is not null)
                    return loaded;
            }
        }
        catch
        {
            // Bozuk ayar dosyasi acilisi engellemesin; varsayilanlara don.
        }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch
        {
            // Ayar yazilamadiysa uygulama calismaya devam etsin.
        }
    }
}
