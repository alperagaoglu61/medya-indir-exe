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
