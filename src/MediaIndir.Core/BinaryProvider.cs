using System.Reflection;

namespace MediaIndir.Core;

/// <summary>
/// Gomulu yt-dlp / ffmpeg / ffprobe ikililerini diske cikarir ve yollarini verir.
/// Kaynaklar bu assembly'de (MediaIndir.Core) gomulu oldugu icin hem konsol
/// hem WPF arayuzu ayni kopyayi kullanir.
/// </summary>
public sealed class BinaryProvider
{
    /// <summary>Uygulama genelinde paylasilan ornek.</summary>
    public static BinaryProvider Default { get; } = new();

    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _hazir;

    public BinaryProvider()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MediaIndir", "bin"))
    {
    }

    public BinaryProvider(string toolsDir) => ToolsDir = toolsDir;

    /// <summary>Ikililerin cikarildigi klasor. yt-dlp'ye --ffmpeg-location olarak verilir.</summary>
    public string ToolsDir { get; }

    public string YtDlpPath => Path.Combine(ToolsDir, "yt-dlp.exe");
    public string FfmpegPath => Path.Combine(ToolsDir, "ffmpeg.exe");
    public string FfprobePath => Path.Combine(ToolsDir, "ffprobe.exe");

    /// <summary>
    /// Eksik ikilileri cikarir. Zaten cikarilmissa hizlica doner.
    /// Es zamanli cagrilarda tek seferlik calisir.
    /// </summary>
    public async Task EnsureToolsAsync(CancellationToken ct = default)
    {
        if (_hazir)
            return;

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_hazir)
                return;

            Directory.CreateDirectory(ToolsDir);

            await ExtractIfMissingAsync("yt-dlp.exe", YtDlpPath, ct).ConfigureAwait(false);
            await ExtractIfMissingAsync("ffmpeg.exe", FfmpegPath, ct).ConfigureAwait(false);
            await ExtractIfMissingAsync("ffprobe.exe", FfprobePath, ct).ConfigureAwait(false);

            _hazir = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static async Task ExtractIfMissingAsync(string resourceName, string destPath, CancellationToken ct)
    {
        // Zaten cikarilmissa tekrar cikarma (hizli acilis icin)
        if (File.Exists(destPath) && new FileInfo(destPath).Length > 0)
            return;

        var asm = typeof(BinaryProvider).Assembly;
        await using var resStream = asm.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Gomulu kaynak bulunamadi: {resourceName}");

        // Once gecici dosyaya yaz, sonra tasi. Yarim kalan cikarma bir daha
        // "var ama bozuk" bir exe birakmasin.
        var tempPath = destPath + ".tmp";

        await using (var fileStream = File.Create(tempPath))
        {
            await resStream.CopyToAsync(fileStream, ct).ConfigureAwait(false);
        }

        File.Move(tempPath, destPath, overwrite: true);
    }
}
