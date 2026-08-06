namespace MediaIndir.Core;

public enum JobState
{
    Queued,
    Probing,
    Running,
    Completed,
    Failed,
    Canceled
}

/// <summary>
/// Kuyruktaki tek bir indirme. Downloader'in olaylarini disari tasir,
/// ustune sira/iptal durumunu ekler. Arayuzden bagimsizdir.
/// </summary>
public sealed class DownloadJob
{
    private readonly CancellationTokenSource _cts = new();

    internal DownloadJob(DownloadRequest request)
    {
        Request = request;
        Platform = PlatformNames.FromUrl(request.Url);
    }

    public Guid Id { get; } = Guid.NewGuid();
    public DownloadRequest Request { get; }
    public string Platform { get; }

    public JobState State { get; private set; } = JobState.Queued;
    public MediaInfo? Info { get; private set; }
    public string? FilePath { get; private set; }
    public string? Error { get; private set; }

    public CancellationToken Token => _cts.Token;

    /// <summary>Yuzde (0-100), ham hiz metni, ham kalan sure metni.</summary>
    public event Action<double, string, string>? ProgressChanged;

    public event Action<string>? StatusChanged;
    public event Action<string>? Completed;
    public event Action<string>? Failed;

    /// <summary>Sira/calisma/bitis durumu degisti.</summary>
    public event Action<JobState>? StateChanged;

    /// <summary>On bilgi (baslik, kapak) geldi.</summary>
    public event Action<MediaInfo>? InfoLoaded;

    public void Cancel()
    {
        if (State is JobState.Completed or JobState.Failed or JobState.Canceled)
            return;

        try
        {
            _cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Is zaten bitmis.
        }
    }

    internal void SetState(JobState state)
    {
        if (State == state)
            return;

        State = state;
        StateChanged?.Invoke(state);
    }

    internal void SetInfo(MediaInfo info)
    {
        Info = info;
        InfoLoaded?.Invoke(info);
    }

    internal void RaiseProgress(double percent, string speed, string eta) =>
        ProgressChanged?.Invoke(percent, speed, eta);

    internal void RaiseStatus(string durum) => StatusChanged?.Invoke(durum);

    internal void RaiseCompleted(string dosyaYolu)
    {
        FilePath = dosyaYolu;
        Completed?.Invoke(dosyaYolu);
    }

    internal void RaiseFailed(string hata)
    {
        Error = hata;
        Failed?.Invoke(hata);
    }

    internal void DisposeCts() => _cts.Dispose();
}

/// <summary>
/// En fazla <see cref="MaxParallel"/> indirmeyi ayni anda calistirir,
/// gerisini sirada bekletir.
/// </summary>
public sealed class DownloadQueue : IDisposable
{
    private readonly SemaphoreSlim _slots;
    private readonly BinaryProvider _binaries;

    public DownloadQueue(int maxParallel = 3, BinaryProvider? binaries = null)
    {
        if (maxParallel < 1)
            throw new ArgumentOutOfRangeException(nameof(maxParallel));

        MaxParallel = maxParallel;
        _slots = new SemaphoreSlim(maxParallel, maxParallel);
        _binaries = binaries ?? BinaryProvider.Default;
    }

    public int MaxParallel { get; }

    /// <summary>Yeni is kuyruga eklendi (arayuz satiri burada olusturulur).</summary>
    public event Action<DownloadJob>? JobAdded;

    /// <summary>Is bitti (basarili, hatali veya iptal).</summary>
    public event Action<DownloadJob>? JobFinished;

    public DownloadJob Enqueue(DownloadRequest request)
    {
        var job = new DownloadJob(request);
        JobAdded?.Invoke(job);

        // Bilerek beklemiyoruz: kuyruk arka planda islenir, cagiran taraf donmaz.
        _ = RunAsync(job);

        return job;
    }

    private async Task RunAsync(DownloadJob job)
    {
        try
        {
            await _slots.WaitAsync(job.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            job.RaiseStatus("İptal edildi");
            job.SetState(JobState.Canceled);
            Finish(job);
            return;
        }

        try
        {
            // On bilgi: baslik ve kapak. Basarisiz olursa indirme yine devam eder.
            job.SetState(JobState.Probing);
            job.RaiseStatus("Bilgi alınıyor");

            var info = await new MediaInfoProbe(_binaries).ProbeAsync(job.Request.Url, job.Token)
                .ConfigureAwait(false);

            if (info is not null)
                job.SetInfo(info);

            job.SetState(JobState.Running);

            var downloader = new Downloader(_binaries);
            downloader.ProgressChanged += job.RaiseProgress;
            downloader.StatusChanged += job.RaiseStatus;
            downloader.Completed += job.RaiseCompleted;
            downloader.Failed += job.RaiseFailed;

            var ok = await downloader.DownloadAsync(job.Request, job.Token).ConfigureAwait(false);

            job.SetState(ok ? JobState.Completed : JobState.Failed);
        }
        catch (OperationCanceledException)
        {
            job.SetState(JobState.Canceled);
        }
        catch (Exception ex)
        {
            job.RaiseFailed(ex.Message);
            job.SetState(JobState.Failed);
        }
        finally
        {
            _slots.Release();
            Finish(job);
        }
    }

    private void Finish(DownloadJob job)
    {
        JobFinished?.Invoke(job);
        job.DisposeCts();
    }

    public void Dispose() => _slots.Dispose();
}
