# MediaIndir

yt-dlp tabanli video/muzik indirici. YouTube, Instagram, TikTok, Twitter/X ve yt-dlp'nin destekledigi diger platformlardan indirme yapar.

Iki arayuz var, ikisi de ayni motoru (`MediaIndir.Core`) kullanir:

- **WPF arayuzu** (`MediaIndirGui.exe`) - Fluent koyu tema, indirme kuyrugu, pano izleme
- **Konsol surumu** (`MediaIndir.exe`) - eski menulu surum, aynen calismaya devam ediyor

## Ozellikler

- Tek `.exe` - yt-dlp, ffmpeg ve ffprobe icine gomulu, ayrica kurulum gerekmez
- MP3 (128/192/256/320 kbps) veya MP4 (480p/720p/1080p)
- Ayni anda en fazla 3 paralel indirme, gerisi sirada bekler
- Panoya kopyalanan link otomatik olarak link kutusuna duser (ayardan kapatilabilir)
- Her satirda ilerleme yuzdesi, hiz ve kalan sure; iptal butonu
- MP3'lerde thumbnail ve metadata otomatik eklenir

## Kullanim (WPF)

1. `MediaIndirGui.exe` calistir - odak dogrudan link kutusunda olur
2. Linki yapistir (veya kopyala; uygulama acikken otomatik dusur)
3. MP4/MP3 ve kalite sec, gerekiyorsa **Klasor** ile hedefi degistir
4. **Indir** (veya Enter)

Indirilenler `<secilen klasor>\<format>-<kalite>` altina kaydedilir; varsayilan
kok klasor `Masaustu\MediaIndirilenler`.

Ayarlar: `%APPDATA%\MediaIndir\settings.json`

## Kullanim (konsol)

`MediaIndir.exe` calistir:

1. Link gir (`http://` veya `https://` ile baslamali)
2. Format sec: `1` MP3, `2` MP4
3. Kalite sec

Cikmak icin `q` yaz.

Ilk calistirmada gomulu araclar sessizce `%LOCALAPPDATA%\MediaIndir\bin\` klasorune cikarilir, sonraki acilislarda tekrar cikarma yapilmaz.

## Alternatif: PowerShell script

`MediaIndir.ps1` konsol surumunun PowerShell karsiligidir. Kullanmak icin `yt-dlp.exe`, `ffmpeg.exe` ve `ffprobe.exe` dosyalarini script ile ayni klasore koy:

```powershell
.\MediaIndir.ps1
```

## Proje yapisi

```
src/MediaIndir.Core/      indirme motoru + gomulu araclar (arayuzden bagimsiz)
src/MediaIndir.Console/   konsol arayuzu
src/MediaIndir.Gui/       WPF arayuzu (WPF-UI, CommunityToolkit.Mvvm)
```

`Downloader` ekrana hicbir sey yazmaz; `ProgressChanged`, `StatusChanged`, `Completed`, `Failed` olaylarini yayinlar ve `CancellationToken` ile iptal edilebilir.

## Derleme (kaynak koddan)

Detay: [NASIL-DERLENIR.md](NASIL-DERLENIR.md). Ozetle (.NET 10 SDK gerekir):

```powershell
dotnet publish src\MediaIndir.Gui -c Release -o out\gui
dotnet publish src\MediaIndir.Console -c Release -o out\konsol
```

Gereksinim: `src\MediaIndir.Core\Tools\` altinda `yt-dlp.exe`, `ffmpeg.exe`, `ffprobe.exe`.

## Notlar

- Antivirus "taninmayan yayimci" uyarisi verebilir - kod imzasiz oldugu icin normal.
- Uretilen exe boyutu buyuktur (GUI ~153 MB, konsol ~124 MB) cunku .NET runtime, ffmpeg ve ffprobe icinde tasinir.
