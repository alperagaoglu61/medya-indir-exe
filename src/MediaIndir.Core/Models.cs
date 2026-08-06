using System.Globalization;

namespace MediaIndir.Core;

public enum MediaFormat
{
    Mp3,
    Mp4
}

/// <summary>Tek bir indirme isteginin tum girdileri.</summary>
public sealed record DownloadRequest(
    string Url,
    MediaFormat Format,
    string Quality,
    string OutputDirectory)
{
    /// <summary>mp3: 320 / 256 / 192 / 128 (kbps)</summary>
    public static readonly string[] Mp3Qualities = ["320", "256", "192", "128"];

    /// <summary>mp4: 1080 / 720 / 480 (p)</summary>
    public static readonly string[] Mp4Qualities = ["1080", "720", "480"];

    public string FormatKey => Format == MediaFormat.Mp3 ? "mp3" : "mp4";

    /// <summary>"mp3 (320kbps)" / "mp4 (1080p)"</summary>
    public string Describe() =>
        $"{FormatKey} ({Quality}{(Format == MediaFormat.Mp3 ? "kbps" : "p")})";
}

public static class DownloadPaths
{
    /// <summary>Konsol surumunun kullandigi kok klasor: Masaustu\MediaIndirilenler</summary>
    public static string DefaultRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        "MediaIndirilenler");

    /// <summary>Kok klasor altinda "mp4-1080" gibi alt klasor yolu uretir.</summary>
    public static string SubFolder(string root, MediaFormat format, string quality) =>
        Path.Combine(root, $"{(format == MediaFormat.Mp3 ? "mp3" : "mp4")}-{quality}");
}

public static class PlatformNames
{
    public static string FromUrl(string url)
    {
        if (url.Contains("youtube.com") || url.Contains("youtu.be")) return "YouTube";
        if (url.Contains("instagram.com")) return "Instagram";
        if (url.Contains("tiktok.com")) return "TikTok";
        if (url.Contains("twitter.com") || url.Contains("x.com")) return "Twitter-X";
        return "Diger";
    }
}

/// <summary>
/// yt-dlp'nin ham ilerleme metinlerini ("18.40MiB/s", "00:24") arayuzde
/// gosterilecek bicime cevirir. Regex yok, sadece son ek eslemesi.
/// </summary>
public static class ProgressFormat
{
    private static readonly (string Suffix, string Display)[] SpeedUnits =
    [
        ("GiB/s", "GB/s"),
        ("MiB/s", "MB/s"),
        ("KiB/s", "KB/s"),
        ("B/s",   "B/s")
    ];

    /// <summary>yt-dlp bilinmeyen degeri "Unknown", "NA" veya "N/A" olarak yazar.</summary>
    private static bool Bilinmiyor(string s) =>
        s.Length == 0
        || s.StartsWith("Unknown", StringComparison.OrdinalIgnoreCase)
        || s.Equals("NA", StringComparison.OrdinalIgnoreCase)
        || s.Equals("N/A", StringComparison.OrdinalIgnoreCase);

    /// <summary>"18.40MiB/s" -> "18,4 MB/s" (mevcut kulturun ondalik ayraci ile)</summary>
    public static string Speed(string raw)
    {
        var s = raw.Trim();
        if (Bilinmiyor(s))
            return "-";

        foreach (var (suffix, display) in SpeedUnits)
        {
            if (!s.EndsWith(suffix, StringComparison.Ordinal))
                continue;

            var number = s[..^suffix.Length].Trim();
            if (double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                return $"{value.ToString("0.#", CultureInfo.CurrentCulture)} {display}";

            return $"{number} {display}";
        }

        return s;
    }

    /// <summary>"00:24" -> "00:24 kaldi", bilinmiyorsa "-"</summary>
    public static string Eta(string raw)
    {
        var s = raw.Trim();
        if (Bilinmiyor(s))
            return "-";

        return $"{s} kaldı";
    }
}
