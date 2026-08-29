// ===========================================================================
// Telif Hakki (c) 2026 Alper Ibrahimagaoglu - Tum Haklari Saklidir.
// Copyright (c) 2026 Alper Ibrahimagaoglu - All Rights Reserved.
//
// Bu dosya tescilli (proprietary) yazilimdir. Yalnizca kisisel ve egitim
// amacli olarak GORUNTULENEBILIR ve DEGISTIRILMEDEN calistirilabilir.
//
// Telif sahibinin yazili izni olmadan YASAKTIR:
//   * Degistirme, uyarlama, turev eser olusturma (No Derivatives)
//   * Kopyalama, yeniden dagitma, aynalama, baska bir depoda/platformda
//     yayimlama (No Redistribution)
//   * Ticari kullanim, satis, kiralama, alt lisanslama (No Commercial Use)
//   * Bu telif basligini kaldirma veya degistirme
//
// Tum kosullar icin depodaki LICENSE dosyasina bakiniz.
// https://github.com/alperagaoglu61/medya-indir-exe
//
// GARANTI YOKTUR. Indirilen icerigin telif haklarina ve ilgili platformlarin
// hizmet sartlarina uygunlugundan yalnizca kullanici sorumludur.
// ===========================================================================
using MediaIndir.Core;

namespace MediaIndir;

/// <summary>
/// Konsol arayuzu. Indirme mantigi MediaIndir.Core icindeki Downloader'da;
/// burada sadece menu ve ekrana yazma var.
/// </summary>
internal static class Program
{
    private static readonly BinaryProvider Binaries = BinaryProvider.Default;

    private static async Task<int> Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        try
        {
            await Binaries.EnsureToolsAsync().ConfigureAwait(false);
            ShowBanner();
            await RunLoopAsync().ConfigureAwait(false);
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

    private static async Task RunLoopAsync()
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
                        await RunMp3FlowAsync(url).ConfigureAwait(false);
                        break;
                    case "2":
                        await RunMp4FlowAsync(url).ConfigureAwait(false);
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

    private static async Task RunMp3FlowAsync(string url)
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

        await IndirAsync(url, MediaFormat.Mp3, kalite).ConfigureAwait(false);
    }

    private static async Task RunMp4FlowAsync(string url)
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

        await IndirAsync(url, MediaFormat.Mp4, kalite).ConfigureAwait(false);
    }

    // ---------------- Indirme ----------------

    private static async Task IndirAsync(string url, MediaFormat format, string kalite)
    {
        var hedefKlasor = DownloadPaths.SubFolder(DownloadPaths.DefaultRoot, format, kalite);
        var request = new DownloadRequest(url, format, kalite, hedefKlasor);

        Console.WriteLine();
        WriteColor($"Platform     : {PlatformNames.FromUrl(url)}", ConsoleColor.Cyan);
        WriteColor($"Format       : {request.Describe()}", ConsoleColor.Cyan);
        WriteColor($"Hedef klasor : {hedefKlasor}", ConsoleColor.Cyan);
        WriteColor("Indirme basliyor...", ConsoleColor.Green);
        Console.WriteLine();

        var downloader = new Downloader(Binaries);
        var sonYuzde = -1;

        downloader.StatusChanged += durum =>
        {
            sonYuzde = -1;
            TemizleSatir();
            WriteColor($"[{durum}]", ConsoleColor.DarkGray);
        };

        downloader.ProgressChanged += (yuzde, hiz, kalan) =>
        {
            // Ayni satiri gunceller; her tam yuzde degisiminde bir kez yazar.
            var tamYuzde = (int)yuzde;
            if (tamYuzde == sonYuzde)
                return;

            sonYuzde = tamYuzde;
            Console.Write($"\r  %{yuzde,5:0.0}  {hiz,-12}  {kalan,-10}");
        };

        downloader.Completed += dosyaYolu =>
        {
            TemizleSatir();
            WriteColor($"Indirme tamamlandi -> {dosyaYolu}", ConsoleColor.Green);
        };

        downloader.Failed += hata =>
        {
            TemizleSatir();
            WriteColor("Indirme sirasinda hata olustu. Linki ve baglantiyi kontrol edin.", ConsoleColor.Red);
            WriteColor(hata, ConsoleColor.DarkRed);
        };

        await downloader.DownloadAsync(request).ConfigureAwait(false);
    }

    private static void TemizleSatir()
    {
        // Ilerleme satirinin uzerine yazip imleci basa al.
        Console.Write("\r" + new string(' ', 45) + "\r");
    }

    private static void WriteColor(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }
}
