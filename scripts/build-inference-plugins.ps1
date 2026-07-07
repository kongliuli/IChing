# 构建解读引擎插件 DLL 并复制到 plugins/（Lab API + 测试共用）
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
    @{ Project = "samples\OpenAiCompatibleEngine\OpenAiCompatibleEngine.csproj"; Name = "OpenAiCompatibleEngine.dll" },
    @{ Project = "samples\LLamaSharpEngine\LLamaSharpEngine.csproj"; Name = "LLamaSharpEngine.dll" }
)

foreach ($p in $plugins) {
    $proj = Join-Path $Root $p.Project
    foreach ($dir in $OutDirs) {
        dotnet build $proj -c Release -o $dir
    }
    Write-Host "Built $($p.Name)"
}

Write-Host "Inference plugins -> $($OutDirs -join ', ')"
