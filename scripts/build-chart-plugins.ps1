# 构建排盘插件 DLL 并复制到 plugins/（Lab API + 测试共用）
$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$OutDirs = @(
    (Join-Path $Root "src\IChing.Lab.Api\plugins"),
    (Join-Path $Root "plugins")
)
foreach ($dir in $OutDirs) {
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
}

$plugins = @(
    @{ Project = "samples\TarotEngines\TarotEngines.csproj"; Name = "TarotEngines.dll" },
    @{ Project = "samples\BaziEngines\BaziEngines.csproj"; Name = "BaziEngines.dll" },
    @{ Project = "samples\LiuyaoEngines\LiuyaoEngines.csproj"; Name = "LiuyaoEngines.dll" },
    @{ Project = "samples\CalendarEngines\CalendarEngines.csproj"; Name = "CalendarEngines.dll" },
    @{ Project = "samples\ChartBridge\ChartBridge.csproj"; Name = "ChartBridge.dll" },
    @{ Project = "samples\SamplePlugin\SamplePlugin.csproj"; Name = "SamplePlugin.dll" }
)

foreach ($p in $plugins) {
    $proj = Join-Path $Root $p.Project
    foreach ($dir in $OutDirs) {
        dotnet build $proj -c Release -o $dir
    }
    Write-Host "Built $($p.Name)"
}

Write-Host "Chart plugins -> $($OutDirs -join ', ')"
