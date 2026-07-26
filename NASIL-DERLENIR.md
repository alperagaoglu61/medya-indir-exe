# MediaIndir - Tek EXE Derleme Talimati

Bu proje, yt-dlp.exe ve ffmpeg.exe'yi kendi icine gomup, calistiginda otomatik
cikaran tek bir .exe uretir. Kullaniciya sadece bu tek dosyayi verirsin.

## 1. Gereksinim: .NET SDK

Bilgisayarinda .NET SDK (8.0) kurulu olmali. Kontrol icin PowerShell'de:
```
dotnet --version
```
Yoksa: https://dotnet.microsoft.com/download adresinden ".NET 8.0 SDK" indir, kur.

## 2. Klasor yapisini hazirla

Bu proje klasorune (MediaIndir.csproj, Program.cs, bu dosya) ek olarak
bir `Tools` klasoru olustur ve icine su iki dosyayi koy:

```
MediaIndirExe/
├── MediaIndir.csproj
├── Program.cs
├── NASIL-DERLENIR.md
└── Tools/
    ├── yt-dlp.exe      <- elindeki dosyayi buraya kopyala
    └── ffmpeg.exe      <- elindeki dosyayi buraya kopyala
```

(Bu iki dosya zaten senin "medya-indir" klasorunde var - oradan kopyalaman yeterli.)

## 3. Derle

Proje klasorunde (MediaIndir.csproj'un oldugu yerde) PowerShell'de:

```powershell
dotnet publish -c Release -o out
```

Bu komut internetten .NET calisma zamani parcalarini indirecek (ilk seferde),
sonra `out` klasorune tek bir `MediaIndir.exe` uretecek.

## 4. Sonuc

`out\MediaIndir.exe` dosyasi - baska hicbir seye ihtiyac duymadan tek basina
calisir. Ilk calistirmada yt-dlp.exe ve ffmpeg.exe'yi sessizce
`%LOCALAPPDATA%\MediaIndir\bin\` klasorune cikarir, sonraki calistirmalarda
tekrar cikarma yapmaz (hizli acilir).

Dosya boyutu byuk olacaktir (~150-200 MB civari) cunku hem .NET calisma
zamanini hem de ffmpeg'i icinde tasiyor - bu normal ve beklenen bir durum.

## Notlar

- Antivirus programlari bazen "taninmayan yayimci" uyarisi verebilir (kod
  imzasiz oldugu icin) - bu zararli oldugu anlamina gelmez, sadece Microsoft
  onayli bir sertifika ile imzalanmadigi icin cikan standart bir uyaridir.
- Ileride GUI (pencereli) versiyona gecmek istersen, bu Program.cs'teki
  Indir() fonksiyonunun mantigini aynen koruyup Console.ReadLine() yerine
  WPF/WinForms buton/dropdown olaylarina baglayabilirsin.
