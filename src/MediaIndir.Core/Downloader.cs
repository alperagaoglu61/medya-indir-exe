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
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace MediaIndir.Core;

/// <summary>
/// yt-dlp surecini calistiran indirme motoru.
///
/// Bu sinif arayuzden bagimsizdir: hicbir yere yazi basmaz, sadece olay yayinlar.
/// Konsol surumu de WPF surumu de ayni motoru kullanir.
/// </summary>
public sealed class Downloader
{
    // Indirme hizi ayarlari.
    //
    // Olcum notu: YouTube'da hiz esas olarak sunucu/CDN tarafinda belirleniyor.
    // Ayni ayarla ayni saatte 23 MB/s de 87 MB/s de olculdu. aria2c ile 16 paralel
    // baglantiya bolmek tutarli kazanc vermedi, bazi videolarda daha yavas oldu;
    // bu yuzden yt-dlp'nin kendi indiricisi kullaniliyor.
    //
    //   -N 8               : parcali (HLS/DASH) akislarda 8 fragment paralel iner.
    //                        Instagram/TikTok gibi siteler icin onemli; YouTube'un
    //                        tek parcali (protocol=https) formatlarinda etkisi yok.
    //   --throttled-rate   : hiz 100 KB/s altina duserse baglanti yeniden kurulur
    //   --retries / --fragment-retries : gecici kopmalarda tekrar dener
    private static readonly string[] HizArgs =
    [
        "-N", "8",
        "--throttled-rate", "100K",
        "--retries", "10",
        "--fragment-retries", "10",
        "--retry-sleep", "1"
    ];

    // Ilerleme satiri bicimi: "  62.3%|18.40MiB/s|00:24"
    // Bastaki "download:" bir asama secicisidir, ciktiya yazilmaz.
    private const string ProgressTemplate =
        "download:%(progress._percent_str)s|%(progress._speed_str)s|%(progress._eta_str)s";

    private readonly BinaryProvider _binaries;

    public Downloader(BinaryProvider? binaries = null) => _binaries = binaries ?? BinaryProvider.Default;

    /// <summary>Yuzde (0-100), hiz metni ("18.40MiB/s"), kalan sure metni ("00:24").</summary>
    public event Action<double, string, string>? ProgressChanged;

    /// <summary>Kullaniciya gosterilecek durum metni ("İndiriliyor", "Dönüştürülüyor"...).</summary>
    public event Action<string>? StatusChanged;

    /// <summary>Basariyla biten indirmenin diskteki son dosya yolu.</summary>
    public event Action<string>? Completed;

    /// <summary>Hata mesaji.</summary>
    public event Action<string>? Failed;

    /// <summary>
    /// Indirmeyi calistirir. Hata durumunda istisna firlatmaz; <see cref="Failed"/>
    /// yayinlar ve false doner. Iptal edilirse OperationCanceledException firlatir.
    /// </summary>
    public async Task<bool> DownloadAsync(DownloadRequest request, CancellationToken ct = default)
    {
        // Sonuc dosyasinin yolunu yt-dlp'nin kendisi yazar; boylece tahmin yapmiyoruz.
        var filePathFile = Path.Combine(Path.GetTempPath(), $"mediaindir-{Guid.NewGuid():N}.path");

        try
        {
            StatusChanged?.Invoke("Hazırlanıyor");
            await _binaries.EnsureToolsAsync(ct).ConfigureAwait(false);

            Directory.CreateDirectory(request.OutputDirectory);

            var psi = BuildStartInfo(request, filePathFile);

            StatusChanged?.Invoke("Bağlanıyor");

            var (exitCode, hataSatirlari) = await RunProcessAsync(psi, ct).ConfigureAwait(false);

            if (exitCode == 0)
            {
                var dosyaYolu = ReadResultPath(filePathFile) ?? request.OutputDirectory;
                StatusChanged?.Invoke("Tamamlandı");
                Completed?.Invoke(dosyaYolu);
                return true;
            }

            var mesaj = hataSatirlari.Count > 0
                ? string.Join(Environment.NewLine, hataSatirlari)
                : $"yt-dlp {exitCode} kodu ile sonlandı. Linki ve bağlantıyı kontrol edin.";

            StatusChanged?.Invoke("Hata");
            Failed?.Invoke(mesaj);
            return false;
        }
        catch (OperationCanceledException)
        {
            StatusChanged?.Invoke("İptal edildi");
            throw;
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke("Hata");
            Failed?.Invoke(ex.Message);
            return false;
        }
        finally
        {
            TryDelete(filePathFile);
        }
    }

    // ---------------- Argumanlar ----------------

    private ProcessStartInfo BuildStartInfo(DownloadRequest request, string filePathFile)
    {
        var outputTemplate = Path.Combine(request.OutputDirectory, "%(title)s.%(ext)s");

        var psi = new ProcessStartInfo
        {
            FileName = _binaries.YtDlpPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = request.OutputDirectory
        };

        var args = psi.ArgumentList;

        if (request.Format == MediaFormat.Mp3)
        {
            args.Add("-x");
            args.Add("--audio-format"); args.Add("mp3");
            args.Add("--audio-quality"); args.Add($"{request.Quality}K");
            args.Add("--embed-thumbnail");
            args.Add("--add-metadata");
        }
        else
        {
            args.Add("-f");
            args.Add($"bestvideo[height<={request.Quality}][vcodec^=avc1]+bestaudio[ext=m4a]/" +
                     $"bestvideo[height<={request.Quality}][ext=mp4]+bestaudio[ext=m4a]/" +
                     $"best[height<={request.Quality}][ext=mp4]/" +
                     $"best[height<={request.Quality}]");
            args.Add("--merge-output-format"); args.Add("mp4");
        }

        args.Add("--no-playlist");
        args.Add("--no-abort-on-error");

        foreach (var hizArg in HizArgs)
            args.Add(hizArg);

        // Satir satir, ayristirilabilir ilerleme
        args.Add("--progress");
        args.Add("--newline");
        args.Add("--progress-template"); args.Add(ProgressTemplate);

        // Son dosya yolunu ayri bir dosyaya yazdir (stdout'a karismasin)
        args.Add("--print-to-file"); args.Add("after_move:filepath"); args.Add(filePathFile);

        args.Add("--ffmpeg-location"); args.Add(_binaries.ToolsDir);
        args.Add("-o"); args.Add(outputTemplate);
        args.Add(request.Url);

        return psi;
    }

    // ---------------- Surec ----------------

    private async Task<(int ExitCode, List<string> HataSatirlari)> RunProcessAsync(
        ProcessStartInfo psi,
        CancellationToken ct)
    {
        var hatalar = new List<string>();

        using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };

        proc.OutputDataReceived += (_, e) =>
        {
            if (e.Data is { Length: > 0 })
                HandleOutputLine(e.Data);
        };

        proc.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not { Length: > 0 })
                return;

            // Son birkac hata satirini sakla; hepsini biriktirip arayuzu bogmayalim.
            lock (hatalar)
            {
                hatalar.Add(e.Data.Trim());
                if (hatalar.Count > 5)
                    hatalar.RemoveAt(0);
            }
        };

        if (!proc.Start())
            throw new InvalidOperationException("yt-dlp başlatılamadı.");

        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        // Iptal istendiginde yt-dlp'yi ve alt sureclerini (ffmpeg) oldur.
        await using var iptalKaydi = ct.Register(() =>
        {
            try
            {
                if (!proc.HasExited)
                    proc.Kill(entireProcessTree: true);
            }
            catch
            {
                // Surec zaten bitmis olabilir; yapacak bir sey yok.
            }
        });

        // ct'yi buraya vermiyoruz: once surecin gercekten olmesini bekliyoruz.
        await proc.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);

        ct.ThrowIfCancellationRequested();

        lock (hatalar)
        {
            return (proc.ExitCode, [.. hatalar]);
        }
    }

    // ---------------- Cikti ayristirma ----------------

    private void HandleOutputLine(string line)
    {
        if (TryParseProgress(line))
            return;

        // Ilerleme disi satirlar: yt-dlp asama basliklari
        var trimmed = line.TrimStart();

        if (trimmed.StartsWith("[download] Destination", StringComparison.Ordinal))
            StatusChanged?.Invoke("İndiriliyor");
        else if (trimmed.StartsWith("[Merger]", StringComparison.Ordinal))
            StatusChanged?.Invoke("Birleştiriliyor");
        else if (trimmed.StartsWith("[ExtractAudio]", StringComparison.Ordinal))
            StatusChanged?.Invoke("Dönüştürülüyor");
        else if (trimmed.StartsWith("[EmbedThumbnail]", StringComparison.Ordinal))
            StatusChanged?.Invoke("Kapak ekleniyor");
        else if (trimmed.StartsWith("[Metadata]", StringComparison.Ordinal))
            StatusChanged?.Invoke("Etiketler yazılıyor");
    }

    /// <summary>
    /// "  62.3%|18.40MiB/s|00:24" satirini ayristirir. Regex kullanilmaz.
    /// </summary>
    private bool TryParseProgress(string line)
    {
        var parts = line.Split('|');
        if (parts.Length != 3)
            return false;

        var percentText = parts[0].Trim();
        if (!percentText.EndsWith('%'))
            return false;

        if (!double.TryParse(
                percentText[..^1],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var percent))
        {
            return false;
        }

        ProgressChanged?.Invoke(percent, parts[1].Trim(), parts[2].Trim());
        return true;
    }

    private static string? ReadResultPath(string filePathFile)
    {
        try
        {
            if (!File.Exists(filePathFile))
                return null;

            // Birden fazla satir olabilir (or. ayri video+ses); sonuncusu nihai dosyadir.
            var satirlar = File.ReadAllLines(filePathFile, Encoding.UTF8);
            for (var i = satirlar.Length - 1; i >= 0; i--)
            {
                var satir = satirlar[i].Trim();
                if (satir.Length > 0)
                    return satir;
            }
        }
        catch
        {
            // Yol okunamadiysa cagiran taraf hedef klasore duser.
        }

        return null;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Gecici dosya silinemediyse onemli degil.
        }
    }
}
