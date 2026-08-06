using System.Windows;
using System.Windows.Controls;
using MediaIndir.Gui.Services;
using MediaIndir.Gui.ViewModels;
using Wpf.Ui.Controls;

namespace MediaIndir.Gui;

/// <summary>
/// Sadece arayuz baglantilari: odak, pano izleyicinin baslatilmasi ve
/// yapistirilan metnin linke indirgenmesi. Mantik MainViewModel'de.
/// </summary>
public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _viewModel;
    private readonly ClipboardMonitor _clipboard;

    public MainWindow(MainViewModel viewModel, ClipboardMonitor clipboard)
    {
        _viewModel = viewModel;
        _clipboard = clipboard;

        InitializeComponent();
        DataContext = viewModel;

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Acilista odak link kutusunda olsun.
        LinkKutusu.Focus();

        // Pano izleme penceresi hazir olduktan sonra baslar.
        _clipboard.Start(this);
    }

    private void LinkKutusu_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Ctrl+V ile yapistirilan metnin icinden linki ayikla.
        _viewModel.NormalizeUrl();
    }
}
