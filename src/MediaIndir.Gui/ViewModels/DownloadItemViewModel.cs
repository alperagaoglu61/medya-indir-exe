using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaIndir.Core;

namespace MediaIndir.Gui.ViewModels;

/// <summary>
/// Kuyruktaki bir satir. Core'daki <see cref="DownloadJob"/> olaylarini dinler
/// ve arayuz ipligine tasir.
/// </summary>
public sealed partial class DownloadItemViewModel : ObservableObject
{
    private readonly DownloadJob _job;

    public DownloadItemViewModel(DownloadJob job)
    {
        _job = job;

        _title = job.Request.Url;
        _statusText = "Sırada";
        _detailLine = $"{job.Platform} · {job.Request.Describe()}";

        job.InfoLoaded += info => OnUi(() =>
        {
            Title = info.Title;
            ThumbnailUrl = info.ThumbnailUrl;
        });

        job.StatusChanged += durum => OnUi(() => StatusText = durum);

        job.ProgressChanged += (yuzde, hiz, kalan) => OnUi(() =>
        {
            Percent = yuzde;
            DetailLine = $"%{yuzde:0} · {ProgressFormat.Speed(hiz)} · {ProgressFormat.Eta(kalan)}";
        });

        job.Completed += yol => OnUi(() =>
        {
            FilePath = yol;
            Percent = 100;
            DetailLine = Path.GetFileName(yol);
        });

        job.Failed += hata => OnUi(() =>
        {
            var ilkSatir = hata.Split('\n')[0].Trim();
            DetailLine = ilkSatir.Length > 0 ? ilkSatir : "Bilinmeyen hata";
        });

        job.StateChanged += state => OnUi(() =>
        {
            State = state;

            if (state == JobState.Canceled)
                DetailLine = "İptal edildi";
        });
    }

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string? _thumbnailUrl;

    [ObservableProperty]
    private string _statusText;

    [ObservableProperty]
    private string _detailLine;

    [ObservableProperty]
    private double _percent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanShowInFolder))]
    [NotifyCanExecuteChangedFor(nameof(ShowInFolderCommand))]
    private string? _filePath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFinished))]
    [NotifyPropertyChangedFor(nameof(CanCancel))]
    [NotifyPropertyChangedFor(nameof(CanShowInFolder))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    [NotifyCanExecuteChangedFor(nameof(ShowInFolderCommand))]
    private JobState _state = JobState.Queued;

    /// <summary>Bitmis satirlar arayuzde soluk gosterilir.</summary>
    public bool IsFinished => State is JobState.Completed or JobState.Failed or JobState.Canceled;

    public bool CanCancel => !IsFinished;

    public bool CanShowInFolder => State == JobState.Completed && FilePath is not null;

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel() => _job.Cancel();

    [RelayCommand(CanExecute = nameof(CanShowInFolder))]
    private void ShowInFolder()
    {
        if (FilePath is null)
            return;

        try
        {
            // Dosyayi Gezgin'de secili olarak ac.
            var hedef = File.Exists(FilePath) ? $"/select,\"{FilePath}\"" : $"\"{FilePath}\"";
            Process.Start(new ProcessStartInfo("explorer.exe", hedef) { UseShellExecute = true });
        }
        catch
        {
            // Gezgin acilamadiysa yapacak bir sey yok.
        }
    }

    private static void OnUi(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;

        if (dispatcher is null || dispatcher.CheckAccess())
            action();
        else
            // BeginInvoke: indirme ipligini bekletmeden arayuze siraya koyar.
            dispatcher.BeginInvoke(action);
    }
}
