# 将 Lab API 的 RWS 牌面图同步到 MAUI App（需先运行 download-rws-tarot-assets.ps1）
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$src = Join-Path $root "src/IChing.Lab.Api/wwwroot/tarot/rws"
$dst = Join-Path $root "src/IChing.Tarot.App/Resources/Images"
New-Item -ItemType Directory -Force -Path $dst | Out-Null

$files = Get-ChildItem -Path $src -Filter "*.jpeg" -ErrorAction SilentlyContinue
if ($files.Count -eq 0) {
    Write-Host "未找到牌面图。请先运行: scripts/download-rws-tarot-assets.ps1"
    exit 1
}

$count = 0
foreach ($f in $files) {
    $assetName = "tarot_$($f.BaseName.Replace('-', '_')).jpeg"
    Copy-Item $f.FullName (Join-Path $dst $assetName) -Force
    $count++
}
Write-Host "已同步 $count 张牌面图到 MAUI Resources/Images/ (tarot_*.jpeg)"
