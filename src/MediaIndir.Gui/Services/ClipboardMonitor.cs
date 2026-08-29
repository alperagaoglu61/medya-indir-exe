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
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace MediaIndir.Gui.Services;

/// <summary>
/// Pano degisikliklerini dinler. Panoya desteklenen bir link kopyalandiginda
/// <see cref="LinkCopied"/> olayini yayinlar.
///
/// Yoklama (polling) yok: Windows'un WM_CLIPBOARDUPDATE mesajina abone olunur.
/// </summary>
public sealed class ClipboardMonitor : IDisposable
{
    private const int WmClipboardUpdate = 0x031D;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    private HwndSource? _source;
    private string? _sonLink;

    /// <summary>Izleme acik mi? Kapaliyken olay yayinlanmaz.</summary>
    public bool Enabled { get; set; } = true;

    public event Action<string>? LinkCopied;

    public void Start(Window window)
    {
        if (_source is not null)
            return;

        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
            return;

        _source = HwndSource.FromHwnd(handle);
        _source?.AddHook(WndProc);
        AddClipboardFormatListener(handle);

        // Uygulama acilirken panoda hazir bir link varsa onu da yakala.
        Oku();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmClipboardUpdate)
            Oku();

        return IntPtr.Zero;
    }

    private void Oku()
    {
        if (!Enabled)
            return;

        try
        {
            if (!Clipboard.ContainsText())
                return;

            var metin = Clipboard.GetText().Trim();
            if (!LinkUtils.IsSupportedLink(metin))
                return;

            // Ayni link tekrar tekrar dusmesin.
            if (metin == _sonLink)
                return;

            _sonLink = metin;
            LinkCopied?.Invoke(metin);
        }
        catch
        {
            // Panoyu baska bir uygulama kilitlemis olabilir; sessizce gec.
        }
    }

    public void Dispose()
    {
        if (_source is null)
            return;

        RemoveClipboardFormatListener(_source.Handle);
        _source.RemoveHook(WndProc);
        _source = null;
    }
}
