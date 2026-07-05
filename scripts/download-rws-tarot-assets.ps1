param(
    [ValidateSet("jsdelivr", "github", "wikimedia")]
    [string]$Source = "jsdelivr"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$target = Join-Path $root "src/IChing.Lab.Api/wwwroot/tarot/rws"
New-Item -ItemType Directory -Force -Path $target | Out-Null

$major = @{
    "00" = "the-fool"; "01" = "the-magician"; "02" = "the-high-priestess"; "03" = "the-empress"
    "04" = "the-emperor"; "05" = "the-hierophant"; "06" = "the-lovers"; "07" = "the-chariot"
    "08" = "strength"; "09" = "the-hermit"; "10" = "wheel-of-fortune"; "11" = "justice"
    "12" = "the-hanged-man"; "13" = "death"; "14" = "temperance"; "15" = "the-devil"
    "16" = "the-tower"; "17" = "the-star"; "18" = "the-moon"; "19" = "the-sun"
    "20" = "judgement"; "21" = "the-world"
}
$rank = @{
    "01" = "ace"; "02" = "two"; "03" = "three"; "04" = "four"; "05" = "five"; "06" = "six"; "07" = "seven"
    "08" = "eight"; "09" = "nine"; "10" = "ten"; "11" = "page"; "12" = "knight"; "13" = "queen"; "14" = "king"
}
$suitSrc = @{ cups = "Cups"; pentacles = "Pents"; swords = "Swords"; wands = "Wands" }

$majorFiles = @{
    "00" = "00_Fool.jpg"; "01" = "01_Magician.jpg"; "02" = "02_High_Priestess.jpg"; "03" = "03_Empress.jpg"
    "04" = "04_Emperor.jpg"; "05" = "05_Hierophant.jpg"; "06" = "06_Lovers.jpg"; "07" = "07_Chariot.jpg"
    "08" = "08_Strength.jpg"; "09" = "09_Hermit.jpg"; "10" = "10_Wheel_of_Fortune.jpg"; "11" = "11_Justice.jpg"
    "12" = "12_Hanged_Man.jpg"; "13" = "13_Death.jpg"; "14" = "14_Temperance.jpg"; "15" = "15_Devil.jpg"
    "16" = "16_Tower.jpg"; "17" = "17_Star.jpg"; "18" = "18_Moon.jpg"; "19" = "19_Sun.jpg"
    "20" = "20_Judgement.jpg"; "21" = "21_World.jpg"
}

$cards = [System.Collections.Generic.List[hashtable]]::new()
foreach ($key in ($major.Keys | Sort-Object)) {
    $cards.Add(@{ src = $majorFiles[$key]; dst = "$($major[$key]).jpeg" })
}
foreach ($suit in @("cups", "pentacles", "swords", "wands")) {
    foreach ($num in ($rank.Keys | Sort-Object)) {
        $cards.Add(@{
            src = "$($suitSrc[$suit])$num.jpg"
            dst = "$($rank[$num])-of-$suit.jpeg"
        })
    }
}

function Get-DownloadUrls {
    param([string]$FileName)
    switch ($Source) {
        "github" {
            return @(
                "https://raw.githubusercontent.com/mixvlad/TarotCards/main/tarot/rider-waite/720px/$FileName"
            )
        }
        "wikimedia" {
            return @() # ponytail: 仅作兜底，见 Download-Wikimedia
        }
        default {
            return @(
                "https://cdn.jsdelivr.net/gh/mixvlad/TarotCards@main/tarot/rider-waite/720px/$FileName",
                "https://raw.githubusercontent.com/mixvlad/TarotCards/main/tarot/rider-waite/720px/$FileName"
            )
        }
    }
}

$headers = @{ "User-Agent" = "IChingLab/1.0 (local RWS asset downloader)" }
$failed = [System.Collections.Generic.List[string]]::new()
$downloaded = 0
$skipped = 0

foreach ($card in $cards) {
    $path = Join-Path $target $card.dst
    if ((Test-Path $path) -and ((Get-Item $path).Length -gt 1000)) {
        $skipped++
        continue
    }

    $ok = $false
    foreach ($url in (Get-DownloadUrls -FileName $card.src)) {
        for ($try = 1; $try -le 3; $try++) {
            try {
                Invoke-WebRequest -Uri $url -OutFile $path -Headers $headers -TimeoutSec 45 -UseBasicParsing
                if ((Test-Path $path) -and ((Get-Item $path).Length -gt 1000)) {
                    $ok = $true
                    break
                }
            }
            catch {
                if ($try -lt 3) { Start-Sleep -Seconds 2 }
            }
        }
        if ($ok) { break }
    }

    if ($ok) {
        $downloaded++
        Write-Host "OK $($card.dst)"
    }
    else {
        $failed.Add($card.dst)
        Write-Warning "FAIL $($card.dst)"
    }
}

$notice = @"
Rider-Waite-Smith tarot card images

Source: mixvlad/TarotCards (GitHub), originally from Wikimedia Commons public domain scans.
Downloaded for local display assets by scripts/download-rws-tarot-assets.ps1.
Mirror: jsdelivr CDN (default) or raw.githubusercontent.com fallback.
"@
Set-Content -Path (Join-Path $target "NOTICE.txt") -Value $notice -Encoding UTF8

$total = (Get-ChildItem -Path $target -Filter "*.jpeg").Count
Write-Host "Done: new=$downloaded skipped=$skipped total=$total/78 source=$Source"

if ($failed.Count -gt 0) {
    throw "Failed $($failed.Count) cards: $($failed -join ', ')"
}

if ($total -ne 78) {
    throw "Expected 78 card images, found $total."
}

Write-Host "Downloaded $total Rider-Waite-Smith tarot images to $target"
