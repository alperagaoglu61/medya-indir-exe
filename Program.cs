using System.Diagnostics;
using System.Reflection;

namespace MediaIndir;

internal static class Program
{
    private static readonly string ToolsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MediaIndir", "bin");

    private static readonly string DownloadRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        "MediaIndirilenler");

    private static string YtDlpPath => Path.Combine(ToolsDir, "yt-dlp.exe");
    private static string FfmpegPath => Path.Combine(ToolsDir, "ffmpeg.exe");

    private static int Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        try
        {
            ExtractEmbeddedTools();
            ShowBanner();
            RunLoop();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.WriteLine($"BEKLENMEYEN HATA: {ex.Message}");
            Console.ResetColor();
        }
        finally
        {
            Console.WriteLine();
            Console.Write("Kapatmak icin Enter'a basin...");
            Console.ReadLine();
        }

        return 0;
    }

    // ---------------- Gomulu araclari cikar ----------------

    private static void ExtractEmbeddedTools()
    {
        Directory.CreateDirectory(ToolsDir);
        ExtractIfMissing("yt-dlp.exe", YtDlpPath);
        ExtractIfMissing("ffmpeg.exe", FfmpegPath);
    }

    private static void ExtractIfMissing(string resourceName, string destPath)
    {
        // Zaten cikarilmissa tekrar cikarma (hizli acilis icin)
        if (File.Exists(destPath) && new FileInfo(destPath).Length > 0)
            return;

        var asm = Assembly.GetExecutingAssembly();
        using var resStream = asm.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Gomulu kaynak bulunamadi: {resourceName}");
        using var fileStream = File.Create(destPath);
        resStream.CopyTo(fileStream);
    }

    // ---------------- Arayuz ----------------

    private static void ShowBanner()
    {
        Console.WriteLine();
        Console.WriteLine("==========================================");
        Console.WriteLine("   Video/Muzik Indirici (yt-dlp tabanli)");
        Console.WriteLine("==========================================");
        Console.WriteLine("Destekler: YouTube, Instagram, TikTok, Twitter/X ve daha fazlasi");
        Console.WriteLine();
    }

    private static void RunLoop()
    {
        while (true)
        {
            Console.Write("Link girin (cikmak icin 'q'): ");
            var url = Console.ReadLine()?.Trim() ?? "";

            if (url.Equals("q", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(url))
                break;

            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                WriteColor("Gecersiz link. http:// veya https:// ile baslamali.", ConsoleColor.Red);
                continue;
            }

            Console.WriteLine();
            Console.WriteLine("Format secin:");
            Console.WriteLine("  [1] MP3 (sadece ses)");
            Console.WriteLine("  [2] MP4 (video)");
            Console.Write("Seciminiz (1/2): ");
            var secim = Console.ReadLine()?.Trim();

            try
            {
                switch (secim)
                {
                    case "1":
                        RunMp3Flow(url);
                        break;
                    case "2":
                        RunMp4Flow(url);
                        break;
                    default:
                        WriteColor("Gecersiz secim, atlaniyor.", ConsoleColor.Yellow);
                        break;
                }
            }
            catch (Exception ex)
            {
                WriteColor($"HATA: {ex.Message}", ConsoleColor.Red);
                WriteColor("Bu linki atlayip devam ediliyor.", ConsoleColor.Yellow);
            }

            Console.WriteLine();
        }

        Console.WriteLine();
        WriteColor("Cikis yapiliyor. Iyi gunler!", ConsoleColor.Cyan);
    }

    private static void RunMp3Flow(string url)
    {
        Console.WriteLine();
        Console.WriteLine("MP3 kalitesi secin:");
        Console.WriteLine("  [1] 128 kbps");
        Console.WriteLine("  [2] 192 kbps");
        Console.WriteLine("  [3] 256 kbps");
        Console.WriteLine("  [4] 320 kbps");
        Console.Write("Seciminiz (1-4): ");
        var secim = Console.ReadLine()?.Trim();

        var kalite = secim switch
        {
            "1" => "128",
            "2" => "192",
            "3" => "256",
            "4" => "320",
            _ => null
        };

        if (kalite is null)
        {
            WriteColor("Gecersiz secim, atlaniyor.", ConsoleColor.Yellow);
            return;
        }

        Indir(url, "mp3", kalite);
    }

    private static void RunMp4Flow(string url)
    {
        Console.WriteLine();
        Console.WriteLine("MP4 kalitesi secin:");
        Console.WriteLine("  [1] 480p");
        Console.WriteLine("  [2] 720p");
        Console.WriteLine("  [3] 1080p");
        Console.Write("Seciminiz (1-3): ");
        var secim = Console.ReadLine()?.Trim();

        var kalite = secim switch
        {
            "1" => "480",
            "2" => "720",
            "3" => "1080",
            _ => null
        };

        if (kalite is null)
        {
            WriteColor("Gecersiz secim, atlaniyor.", ConsoleColor.Yellow);
            return;
        }

        Indir(url, "mp4", kalite);
    }

    // ---------------- Indirme ----------------

    private static string GetPlatformAdi(string url)
    {
        if (url.Contains("youtube.com") || url.Contains("youtu.be")) return "YouTube";
        if (url.Contains("instagram.com")) return "Instagram";
        if (url.Contains("tiktok.com")) return "TikTok";
        if (url.Contains("twitter.com") || url.Contains("x.com")) return "Twitter-X";
        return "Diger";
    }

    private static void Indir(string url, string format, string kalite)
    {
        var platform = GetPlatformAdi(url);
        var hedefKlasor = Path.Combine(DownloadRoot, $"{format}-{kalite}");
        Directory.CreateDirectory(hedefKlasor);

        Console.WriteLine();
        WriteColor($"Platform     : {platform}", ConsoleColor.Cyan);
        WriteColor($"Format       : {format} ({kalite}{(format == "mp3" ? "kbps" : "p")})", ConsoleColor.Cyan);
        WriteColor($"Hedef klasor : {hedefKlasor}", ConsoleColor.Cyan);
        WriteColor("Indirme basliyor...", ConsoleColor.Green);
        Console.WriteLine();

        var outputTemplate = Path.Combine(hedefKlasor, "%(title)s.%(ext)s");

        string args = format == "mp3"
            ? $"-x --audio-format mp3 --audio-quality {kalite}K --embed-thumbnail --add-metadata " +
              $"--no-playlist --no-abort-on-error " +
              $"--ffmpeg-location \"{ToolsDir}\" -o \"{outputTemplate}\" \"{url}\""
            : $"-f \"bestvideo[height<={kalite}][vcodec^=avc1]+bestaudio[ext=m4a]/bestvideo[height<={kalite}][ext=mp4]+bestaudio[ext=m4a]/best[height<={kalite}][ext=mp4]/best[height<={kalite}]\" " +
              $"--merge-output-format mp4 --no-playlist --no-abort-on-error " +
              $"--ffmpeg-location \"{ToolsDir}\" -o \"{outputTemplate}\" \"{url}\"";

        var psi = new ProcessStartInfo
        {
            FileName = YtDlpPath,
            Arguments = args,
            UseShellExecute = false,
        };

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("yt-dlp baslatilamadi.");
        proc.WaitForExit();

        if (proc.ExitCode == 0)
        {
            WriteColor($"Indirme tamamlandi -> {hedefKlasor}", ConsoleColor.Green);
        }
        else
        {
            WriteColor("Indirme sirasinda hata olustu. Linki ve baglantiyi kontrol edin.", ConsoleColor.Red);
        }
    }

    private static void WriteColor(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }
}
