<#
.SYNOPSIS
    YouTube, Instagram, TikTok gibi platformlardan video/ses indirme scripti.

.DESCRIPTION
    yt-dlp kullanarak verilen linkten mp3 (ses) veya mp4 (video) olarak indirme yapar.
    Gereksinimler: yt-dlp.exe ve ffmpeg.exe/ffprobe.exe.
    Bu dosyalari PATH'e eklemene gerek yok - script'in bulundugu klasore
    koyman yeterli, script onlari otomatik bulur.

.NOTES
    Indirme adresleri:
        yt-dlp  : https://github.com/yt-dlp/yt-dlp/releases/latest  (yt-dlp.exe)
        ffmpeg  : https://www.gyan.dev/ffmpeg/builds/  (release essentials zip, "bin" klasorundeki
                  ffmpeg.exe ve ffprobe.exe dosyalarini bu script ile ayni klasore koy)
#>

# ---------------- Ayarlar ----------------
$ScriptDir    = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$DownloadRoot = Join-Path $env:USERPROFILE "Desktop\MediaIndirilenler"

# yt-dlp ve ffmpeg once script klasorunde aranir, yoksa PATH'e bakilir
$LocalYtDlp  = Join-Path $ScriptDir "yt-dlp.exe"
$LocalFfmpeg = Join-Path $ScriptDir "ffmpeg.exe"

$YtDlpExe  = if (Test-Path $LocalYtDlp) { $LocalYtDlp } else { "yt-dlp" }
$FfmpegDir = if (Test-Path $LocalFfmpeg) { $ScriptDir } else { $null }

# Indirme hizi ayarlari.
#
# Olcum notu: YouTube'da hiz esas olarak sunucu/CDN tarafinda belirleniyor.
# Ayni ayarla ayni saatte 23 MB/s de 87 MB/s de olculdu. aria2c ile 16 paralel
# baglantiya bolmek tutarli kazanc vermedi, bazi videolarda daha yavas oldu;
# bu yuzden yt-dlp'nin kendi indiricisi kullaniliyor.
#
#   -N 8               : parcali (HLS/DASH) akislarda 8 fragment paralel iner.
#                        Instagram/TikTok gibi siteler icin onemli; YouTube'un
#                        tek parcali (protocol=https) formatlarinda etkisi yok.
#   --throttled-rate   : hiz 100 KB/s altina duserse baglanti yeniden kurulur
#   --retries / --fragment-retries : gecici kopmalarda tekrar dener
$HizArgs = @(
    "-N", "8",
    "--throttled-rate", "100K",
    "--retries", "10",
    "--fragment-retries", "10",
    "--retry-sleep", "1"
)

# ---------------- Fonksiyonlar ----------------

function Test-Gereksinimler {
    $eksik = @()

    $ytdlpVarMi = (Test-Path $LocalYtDlp) -or (Get-Command yt-dlp -ErrorAction SilentlyContinue)
    if (-not $ytdlpVarMi) { $eksik += "yt-dlp" }

    $ffmpegVarMi = (Test-Path $LocalFfmpeg) -or (Get-Command ffmpeg -ErrorAction SilentlyContinue)
    if (-not $ffmpegVarMi) { $eksik += "ffmpeg" }

    if ($eksik.Count -gt 0) {
        Write-Host ""
        Write-Host "HATA: Su araclar bulunamadi: $($eksik -join ', ')" -ForegroundColor Red
        Write-Host ""
        Write-Host "Cozum: asagidaki dosyalari indirip bu script (.ps1/.bat) ile AYNI KLASORE koy:" -ForegroundColor Yellow
        Write-Host "  yt-dlp.exe  -> https://github.com/yt-dlp/yt-dlp/releases/latest"
        Write-Host "  ffmpeg.exe ve ffprobe.exe -> https://www.gyan.dev/ffmpeg/builds/ (essentials zip icindeki bin klasorunden)"
        Write-Host ""
        Write-Host "Script klasoru: $ScriptDir" -ForegroundColor Cyan
        Write-Host ""
        return $false
    }
    return $true
}

function Get-KlasorHazirla {
    param([string]$AltKlasor)
    $hedef = Join-Path $DownloadRoot $AltKlasor
    if (-not (Test-Path $hedef)) {
        New-Item -ItemType Directory -Path $hedef -Force | Out-Null
    }
    return $hedef
}

function Get-PlatformAdi {
    param([string]$Url)
    if ($Url -match "youtube\.com|youtu\.be") { return "YouTube" }
    if ($Url -match "instagram\.com") { return "Instagram" }
    if ($Url -match "tiktok\.com") { return "TikTok" }
    if ($Url -match "twitter\.com|x\.com") { return "Twitter-X" }
    return "Diger"
}

function Invoke-Indirme {
    param(
        [string]$Url,
        [string]$Format,    # "mp3" veya "mp4"
        [string]$Kalite     # mp3: "128","192","256","320" | mp4: "480","720","1080"
    )

    $platform = Get-PlatformAdi -Url $Url
    $hedefKlasor = Get-KlasorHazirla -AltKlasor "$Format-$Kalite"

    Write-Host ""
    Write-Host "Platform     : $platform" -ForegroundColor Cyan
    Write-Host "Format       : $Format ($Kalite$(if ($Format -eq 'mp3') { 'kbps' } else { 'p' }))" -ForegroundColor Cyan
    Write-Host "Hedef klasor : $hedefKlasor" -ForegroundColor Cyan
    Write-Host "Indirme basliyor..." -ForegroundColor Green
    Write-Host ""

    $outputTemplate = Join-Path $hedefKlasor "%(title)s.%(ext)s"
    $ffmpegArgs = if ($FfmpegDir) { @("--ffmpeg-location", $FfmpegDir) } else { @() }

    if ($Format -eq "mp3") {
        & $YtDlpExe -x --audio-format mp3 --audio-quality "${Kalite}K" `
            --embed-thumbnail --add-metadata `
            --no-playlist --no-abort-on-error `
            @HizArgs `
            @ffmpegArgs `
            -o $outputTemplate `
            $Url
    }
    else {
        $formatSecimi = "bestvideo[height<=$Kalite][vcodec^=avc1]+bestaudio[ext=m4a]/bestvideo[height<=$Kalite][ext=mp4]+bestaudio[ext=m4a]/best[height<=$Kalite][ext=mp4]/best[height<=$Kalite]"
        & $YtDlpExe -f $formatSecimi `
            --merge-output-format mp4 --no-playlist --no-abort-on-error `
            @HizArgs `
            @ffmpegArgs `
            -o $outputTemplate `
            $Url
    }

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "Indirme tamamlandi -> $hedefKlasor" -ForegroundColor Green
    }
    else {
        Write-Host ""
        Write-Host "Indirme sirasinda hata olustu. Linki ve baglantiyi kontrol edin." -ForegroundColor Red
    }
}

function Show-Menu {
    Write-Host ""
    Write-Host "==========================================" -ForegroundColor DarkGray
    Write-Host "   Video/Muzik Indirici (yt-dlp tabanli)" -ForegroundColor White
    Write-Host "==========================================" -ForegroundColor DarkGray
    Write-Host "Destekler: YouTube, Instagram, TikTok, Twitter/X, ve yt-dlp'nin desteklidigi diger siteler"
    Write-Host ""
}

# ---------------- Ana Akis ----------------

try {

if (-not (Test-Gereksinimler)) {
    Read-Host "Cikmak icin Enter'a basin"
    exit 1
}

Show-Menu

while ($true) {
    $url = Read-Host "Link girin (cikmak icin 'q')"
    if ($url -eq "q" -or [string]::IsNullOrWhiteSpace($url)) {
        break
    }

    if ($url -notmatch "^https?://") {
        Write-Host "Gecersiz link. http:// veya https:// ile baslamali." -ForegroundColor Red
        continue
    }

    try {

    Write-Host ""
    Write-Host "Format secin:"
    Write-Host "  [1] MP3 (sadece ses)"
    Write-Host "  [2] MP4 (video)"
    $secim = Read-Host "Seciminiz (1/2)"

    switch ($secim) {
        "1" {
            Write-Host ""
            Write-Host "MP3 kalitesi secin:"
            Write-Host "  [1] 128 kbps"
            Write-Host "  [2] 192 kbps"
            Write-Host "  [3] 256 kbps"
            Write-Host "  [4] 320 kbps"
            $kaliteSecim = Read-Host "Seciminiz (1-4)"
            $kaliteMap = @{ "1" = "128"; "2" = "192"; "3" = "256"; "4" = "320" }
            if ($kaliteMap.ContainsKey($kaliteSecim)) {
                Invoke-Indirme -Url $url -Format "mp3" -Kalite $kaliteMap[$kaliteSecim]
            }
            else {
                Write-Host "Gecersiz secim, atlaniyor." -ForegroundColor Yellow
            }
        }
        "2" {
            Write-Host ""
            Write-Host "MP4 kalitesi secin:"
            Write-Host "  [1] 480p"
            Write-Host "  [2] 720p"
            Write-Host "  [3] 1080p"
            $kaliteSecim = Read-Host "Seciminiz (1-3)"
            $kaliteMap = @{ "1" = "480"; "2" = "720"; "3" = "1080" }
            if ($kaliteMap.ContainsKey($kaliteSecim)) {
                Invoke-Indirme -Url $url -Format "mp4" -Kalite $kaliteMap[$kaliteSecim]
            }
            else {
                Write-Host "Gecersiz secim, atlaniyor." -ForegroundColor Yellow
            }
        }
        default { Write-Host "Gecersiz secim, atlaniyor." -ForegroundColor Yellow }
    }
    }
    catch {
        Write-Host ""
        Write-Host "HATA: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Bu linki atlayip devam ediliyor." -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Cikis yapiliyor. Iyi gunler!" -ForegroundColor Cyan

}
catch {
    Write-Host ""
    Write-Host "BEKLENMEYEN HATA: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor DarkRed
}
finally {
    Write-Host ""
    Read-Host "Kapatmak icin Enter'a basin"
}
