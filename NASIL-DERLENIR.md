# MediaIndir - Derleme Talimati

Bu cozum iki arayuz uretir; ikisi de ayni indirme motorunu (MediaIndir.Core)
kullanir:

| Proje | Cikti | Ne |
|---|---|---|
| `src/MediaIndir.Core` | kutuphane | Downloader, BinaryProvider, kuyruk. Gomulu yt-dlp/ffmpeg/ffprobe burada. |
| `src/MediaIndir.Console` | `MediaIndir.exe` | Konsol arayuzu (eski surum) |
| `src/MediaIndir.Gui` | `MediaIndirGui.exe` | WPF arayuzu (WPF-UI / Fluent, koyu tema) |

## 1. Gereksinim: .NET SDK

.NET 10 SDK kurulu olmali (projeler `net10.0-windows` hedefler). Kontrol:

```powershell
dotnet --list-sdks
```

> Not: Bu makinede calisan SDK `%USERPROFILE%\.dotnet\dotnet.exe` altinda.
> `C:\Program Files\dotnet\dotnet.exe` SDK icermiyor, o yuzden `dotnet` komutu
> "No .NET SDKs were found" diyorsa tam yolu kullan:
> `& "$env:USERPROFILE\.dotnet\dotnet.exe" build ...`

## 2. Klasor yapisini hazirla

Gomulu araclar `src/MediaIndir.Core/Tools/` altinda beklenir (git'e dahil
degildir, boyutlari GitHub'in 100 MB limitini asar):

```
medya-indir-exe/
├── MediaIndir.slnx
└── src/
    ├── MediaIndir.Core/
    │   ├── Tools/
    │   │   ├── yt-dlp.exe     <- elindeki dosyayi buraya kopyala
    │   │   ├── ffmpeg.exe     <- elindeki dosyayi buraya kopyala
    │   │   └── ffprobe.exe    <- elindeki dosyayi buraya kopyala
    │   └── *.cs
    ├── MediaIndir.Console/
    └── MediaIndir.Gui/
```

`ffprobe.exe` sart: yt-dlp MP3'e cevirirken ve kapak gomerken onu kullanir.

## 3. Derle

```powershell
$dotnet = "$env:USERPROFILE\.dotnet\dotnet.exe"

# WPF arayuzu
& $dotnet publish src\MediaIndir.Gui -c Release -o out\gui

# Konsol surumu
& $dotnet publish src\MediaIndir.Console -c Release -o out\konsol
```

Her iki proje de `PublishSingleFile` + `SelfContained` ayarli: ciktilar
hedef makinede .NET kurulu olmadan calisir.

## 4. Sonuc

- `out\gui\MediaIndirGui.exe`
- `out\konsol\MediaIndir.exe`

Ilk calistirmada gomulu araclar sessizce `%LOCALAPPDATA%\MediaIndir\bin\`
klasorune cikarilir; sonraki acilislarda tekrar cikarilmaz (hizli acilis).

WPF surumunun ayarlari (indirme klasoru, pano izleme, son secilen format ve
kalite) `%APPDATA%\MediaIndir\settings.json` dosyasinda tutulur.

Dosya boyutu buyuk olur (GUI ~153 MB, konsol ~124 MB) cunku .NET calisma zamani, ffmpeg ve
ffprobe icinde tasinir - normal ve beklenen bir durum.

## Notlar

- Antivirus programlari bazen "taninmayan yayimci" uyarisi verebilir (kod
  imzasiz oldugu icin) - zararli oldugu anlamina gelmez.
- Indirme mantigi tek yerde: `src/MediaIndir.Core/Downloader.cs`. Bu sinif
  ekrana hicbir sey yazmaz, sadece olay yayinlar (`ProgressChanged`,
  `StatusChanged`, `Completed`, `Failed`). Yeni bir arayuz eklemek istersen
  bu olaylara baglanman yeterli.
