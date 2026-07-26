# MediaIndir

yt-dlp tabanli, tek exe halinde calisan video/muzik indirici. YouTube, Instagram, TikTok, Twitter/X ve yt-dlp'nin destekledigi diger platformlardan indirme yapar.

## Ozellikler

- Tek `.exe` dosyasi - yt-dlp.exe ve ffmpeg.exe icine gomulu, ayrica kurulum gerekmez
- MP3 (128/192/256/320 kbps) veya MP4 (480p/720p/1080p) olarak indirme
- Indirilenler otomatik olarak `Masaustu\MediaIndirilenler\<format>-<kalite>` klasorune kaydedilir
- MP3'lerde thumbnail ve metadata otomatik eklenir

## Kullanim

`MediaIndir.exe` dosyasini calistir:

1. Link gir (`http://` veya `https://` ile baslamali)
2. Format sec: `1` MP3, `2` MP4
3. Kalite sec
4. Indirme tamamlaninca dosya hedef klasorde olur

Cikmak icin `q` yaz.

Ilk calistirmada yt-dlp.exe ve ffmpeg.exe sessizce `%LOCALAPPDATA%\MediaIndir\bin\` klasorune cikarilir, sonraki acilislarda tekrar cikarma yapilmaz.

## Alternatif: PowerShell script

`MediaIndir.ps1` ayni islevi PowerShell uzerinden saglar. Kullanmak icin `yt-dlp.exe`, `ffmpeg.exe` ve `ffprobe.exe` dosyalarini script ile ayni klasore koy:

```powershell
.\MediaIndir.ps1
```

## Derleme (kaynak koddan)

Detayli talimat icin [NASIL-DERLENIR.md](NASIL-DERLENIR.md) dosyasina bak. Ozetle:

```powershell
dotnet publish -c Release -o out
```

Gereksinim: .NET SDK, `Tools\yt-dlp.exe` ve `Tools\ffmpeg.exe` dosyalari.

## Notlar

- Antivirus "taninmayan yayimci" uyarisi verebilir - kod imzasiz oldugu icin normal, zararli anlamina gelmez.
- Uretilen exe boyutu buyuktur (~150-200 MB) cunku .NET runtime ve ffmpeg icinde tasinir.
