using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace MediaIndir.Core;

/// <summary>Kuyruk satirinda gosterilecek on bilgi.</summary>
public sealed record MediaInfo(
    string Title,
    string? ThumbnailUrl,
    TimeSpan? Duration,
    string? Uploader);

/// <summary>
/// Indirmeden once yt-dlp --dump-json ile baslik/kapak/sure bilgisini ceker.
/// Basarisiz olursa null doner; indirme yine de yapilabilir.
/// </summary>
public sealed class MediaInfoProbe
{
    private readonly BinaryProvider _binaries;

    public MediaInfoProbe(BinaryProvider? binaries = null) => _binaries = binaries ?? BinaryProvider.Default;

    public async Task<MediaInfo?> ProbeAsync(string url, CancellationToken ct = default)
    {
        try
        {
            await _binaries.EnsureToolsAsync(ct).ConfigureAwait(false);

            var psi = new ProcessStartInfo
            {
                FileName = _binaries.YtDlpPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            psi.ArgumentList.Add("--dump-json");
            psi.ArgumentList.Add("--no-playlist");
            psi.ArgumentList.Add("--skip-download");
            psi.ArgumentList.Add("--no-warnings");
            psi.ArgumentList.Add("--socket-timeout");
            psi.ArgumentList.Add("15");
            psi.ArgumentList.Add(url);

            using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            if (!proc.Start())
                return null;

            await using var iptalKaydi = ct.Register(() =>
            {
                try
                {
                    if (!proc.HasExited)
                        proc.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Surec zaten bitmis olabilir.
                }
            });

            var stdoutTask = proc.StandardOutput.ReadToEndAsync(CancellationToken.None);
            var stderrTask = proc.StandardError.ReadToEndAsync(CancellationToken.None);

            await proc.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();

            var json = await stdoutTask.ConfigureAwait(false);
            _ = await stderrTask.ConfigureAwait(false);

            if (proc.ExitCode != 0 || json.Length == 0)
                return null;

            return Parse(json);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // On bilgi olmadan da indirme yapilabilir.
            return null;
        }
    }

    private static MediaInfo? Parse(string json)
    {
        // --dump-json tek satir JSON basar; birden fazla gelirse ilkini al.
        var firstLine = json.AsSpan();
        var newlineIndex = firstLine.IndexOf('\n');
        if (newlineIndex >= 0)
            firstLine = firstLine[..newlineIndex];

        using var doc = JsonDocument.Parse(firstLine.ToString());
        var root = doc.RootElement;

        var title = root.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String
            ? t.GetString() ?? ""
            : "";

        if (title.Length == 0)
            return null;

        var thumb = root.TryGetProperty("thumbnail", out var th) && th.ValueKind == JsonValueKind.String
            ? th.GetString()
            : null;

        TimeSpan? duration = root.TryGetProperty("duration", out var d) && d.ValueKind == JsonValueKind.Number
            ? TimeSpan.FromSeconds(d.GetDouble())
            : null;

        var uploader = root.TryGetProperty("uploader", out var u) && u.ValueKind == JsonValueKind.String
            ? u.GetString()
            : null;

        return new MediaInfo(title, thumb, duration, uploader);
    }
}
