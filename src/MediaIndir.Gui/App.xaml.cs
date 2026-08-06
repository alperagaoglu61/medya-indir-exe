using System.Windows;
using MediaIndir.Gui.Services;
using MediaIndir.Gui.ViewModels;

namespace MediaIndir.Gui;

public partial class App : Application
{
    private ClipboardMonitor? _clipboard;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settings = AppSettings.Load();
        _clipboard = new ClipboardMonitor { Enabled = settings.ClipboardWatch };

        var window = new MainWindow(new MainViewModel(settings, _clipboard), _clipboard);
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _clipboard?.Dispose();
        base.OnExit(e);
    }
}
