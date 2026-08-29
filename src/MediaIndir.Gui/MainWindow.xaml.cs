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
