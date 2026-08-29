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
namespace MediaIndir.Gui.Services;

public static class LinkUtils
{
    /// <summary>http/https ile baslayan, bosluk icermeyen tek parca metin mi?</summary>
    public static bool IsSupportedLink(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var s = text.Trim();

        if (!s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !s.Contains(' ') && !s.Contains('\n') && s.Length > 11;
    }

    /// <summary>
    /// Yapistirilan metnin icinden ilk linki cikarir. Metin duz link ise aynen doner.
    /// Regex yok: "http" konumundan ilk bosluga kadar okur.
    /// </summary>
    public static string? ExtractLink(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var s = text.Trim();
        if (IsSupportedLink(s))
            return s;

        var index = s.IndexOf("http", StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return null;

        var parca = s[index..];
        var son = parca.AsSpan().IndexOfAny(' ', '\r', '\n');
        if (son >= 0)
            parca = parca[..son];

        return IsSupportedLink(parca) ? parca : null;
    }
}
