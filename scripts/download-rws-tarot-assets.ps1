$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$target = Join-Path $root "src/IChing.Lab.Api/wwwroot/tarot/rws"
New-Item -ItemType Directory -Force -Path $target | Out-Null

$api = "https://commons.wikimedia.org/w/api.php?action=query&generator=categorymembers&gcmtitle=Category:Rider-Waite%20tarot%20deck%20(Roses%20%26%20Lilies)&gcmlimit=200&prop=imageinfo&iiprop=url%7Cextmetadata&format=json"
$json = Invoke-RestMethod -Uri $api -Headers @{ "User-Agent" = "IChingLab/1.0 (local asset downloader)" }

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

$downloaded = 0
$pages = $json.query.pages.PSObject.Properties.Value
foreach ($page in $pages) {
    $title = $page.title
    if ($title -notmatch "^File:RWS1909 - (.+)\.jpeg$") { continue }

    $name = $Matches[1]
    if ($name -match "^(\d{2}) ") {
        if (-not $major.ContainsKey($Matches[1])) { continue }
        $fileName = "$($major[$Matches[1]]).jpeg"
    }
    elseif ($name -match "^(Cups|Pentacles|Swords|Wands) (\d{2})$") {
        if (-not $rank.ContainsKey($Matches[2])) { continue }
        $fileName = "$($rank[$Matches[2]])-of-$($Matches[1].ToLowerInvariant()).jpeg"
    }
    else {
        continue
    }

    $path = Join-Path $target $fileName
    if (Test-Path $path) {
        $downloaded++
        continue
    }

    $url = $page.imageinfo[0].url
    for ($try = 1; $try -le 5; $try++) {
        try {
            Invoke-WebRequest -Uri $url -OutFile $path -Headers @{ "User-Agent" = "IChingLab/1.0 (local asset downloader; Wikimedia Commons public-domain tarot assets)" }
            break
        }
        catch {
            if ($try -eq 5) { throw }
            Start-Sleep -Seconds (60 * $try)
        }
    }
    Start-Sleep -Seconds 8
    $downloaded++
}

$notice = @"
Rider-Waite-Smith tarot card images

Source: Wikimedia Commons, Category:Rider-Waite tarot deck (Roses & Lilies)
Original deck: 1909 Waite-Smith Tarot, illustrated by Pamela Colman Smith.
Commons metadata for the downloaded card files marks them as Public domain / CC-PD-Mark.
Downloaded for local display assets by scripts/download-rws-tarot-assets.ps1.
"@
Set-Content -Path (Join-Path $target "NOTICE.txt") -Value $notice -Encoding UTF8

if ($downloaded -ne 78) {
    throw "Expected 78 card images, downloaded $downloaded."
}

Write-Host "Downloaded $downloaded Rider-Waite-Smith tarot images to $target"
