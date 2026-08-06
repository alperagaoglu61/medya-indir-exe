using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaIndir.Core;
using MediaIndir.Gui.Services;
using Microsoft.Win32;

namespace MediaIndir.Gui.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly AppSettings _settings;
    private readonly DownloadQueue _queue;
    private readonly ClipboardMonitor _clipboard;

    public MainViewModel(AppSettings settings, ClipboardMonitor clipboard)
    {
        _settings = settings;
        _clipboard = clipboard;

        // Ayni anda en fazla 3 indirme; gerisi sirada bekler.
        _queue = new DownloadQueue(maxParallel: 3);

        _outputFolder = settings.OutputFolder;
        _clipboardWatch = settings.ClipboardWatch;
        _isMp4 = !string.Equals(settings.Format, "mp3", StringComparison.OrdinalIgnoreCase);

        Qualities = new ObservableCollection<string>(
            IsMp4 ? DownloadRequest.Mp4Qualities : DownloadRequest.Mp3Qualities);

        _selectedQuality = IsMp4 ? settings.Mp4Quality : settings.Mp3Quality;
        if (!Qualities.Contains(_selectedQuality))
            _selectedQuality = Qualities[0];

        _clipboard.Enabled = settings.ClipboardWatch;
        _clipboard.LinkCopied += link => Url = link;
    }

    public ObservableCollection<DownloadItemViewModel> Items { get; } = [];

    public ObservableCollection<string> Qualities { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    private string _url = "";

    [ObservableProperty]
    private string _outputFolder;

    [ObservableProperty]
    private bool _isMp4;

    [ObservableProperty]
    private string _selectedQuality;

    [ObservableProperty]
    private bool _clipboardWatch;

    /// <summary>MP3/MP4 dugmeleri icin ters bagli ikiz.</summary>
    public bool IsMp3
    {
        get => !IsMp4;
        set => IsMp4 = !value;
    }

    /// <summary>Kalite etiketi: "1080p" veya "320 kbps" gibi.</summary>
    public string QualitySuffix => IsMp4 ? "p" : " kbps";

    public bool HasItems => Items.Count > 0;

    partial void OnIsMp4Changed(bool value)
    {
        // Format degisince kalite listesi de degisir.
        var yeni = value ? DownloadRequest.Mp4Qualities : DownloadRequest.Mp3Qualities;

        Qualities.Clear();
        foreach (var q in yeni)
            Qualities.Add(q);

        SelectedQuality = value ? _settings.Mp4Quality : _settings.Mp3Quality;
        if (!Qualities.Contains(SelectedQuality))
            SelectedQuality = Qualities[0];

        _settings.Format = value ? "mp4" : "mp3";
        _settings.Save();

        OnPropertyChanged(nameof(IsMp3));
        OnPropertyChanged(nameof(QualitySuffix));
    }

    partial void OnSelectedQualityChanged(string value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        if (IsMp4)
            _settings.Mp4Quality = value;
        else
            _settings.Mp3Quality = value;

        _settings.Save();
    }

    partial void OnClipboardWatchChanged(bool value)
    {
        _clipboard.Enabled = value;
        _settings.ClipboardWatch = value;
        _settings.Save();
    }

    partial void OnOutputFolderChanged(string value)
    {
        _settings.OutputFolder = value;
        _settings.Save();
    }

    private bool CanDownload => LinkUtils.IsSupportedLink(Url);

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private void Download()
    {
        var link = LinkUtils.ExtractLink(Url);
        if (link is null)
            return;

        var format = IsMp4 ? MediaFormat.Mp4 : MediaFormat.Mp3;
        var hedef = DownloadPaths.SubFolder(OutputFolder, format, SelectedQuality);

        var job = _queue.Enqueue(new DownloadRequest(link, format, SelectedQuality, hedef));

        // Yeni satir en uste gelsin.
        Items.Insert(0, new DownloadItemViewModel(job));
        OnPropertyChanged(nameof(HasItems));

        Url = "";
    }

    [RelayCommand]
    private void ChooseFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "İndirme klasörünü seç",
            InitialDirectory = Directory.Exists(OutputFolder) ? OutputFolder : DownloadPaths.DefaultRoot
        };

        if (dialog.ShowDialog() == true)
            OutputFolder = dialog.FolderName;
    }

    /// <summary>Yapistirma sonrasi metinden linki ayiklar.</summary>
    public void NormalizeUrl()
    {
        var link = LinkUtils.ExtractLink(Url);
        if (link is not null && link != Url)
            Url = link;
    }
}
