param(
    [string]$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
)

$link = 'C:\IChingDev'
$repo = (Resolve-Path -LiteralPath $Repo).Path.TrimEnd('\')

function Get-JunctionTarget([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    $item = Get-Item -LiteralPath $Path -Force
    if (-not ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) { return $null }
    $dest = $item.Target | Select-Object -First 1
    if (-not $dest) { return $null }
    return (Resolve-Path -LiteralPath $dest).Path.TrimEnd('\')
}

$current = Get-JunctionTarget $link
if ($current -eq $repo) {
    Write-Host "junction OK: $link -> $repo"
    exit 0
}

if (Test-Path -LiteralPath $link) {
    if ($null -ne $current) {
        Write-Host "junction rebuild: $link ($current -> $repo)"
        cmd /c rmdir "$link"
        if ($LASTEXITCODE -ne 0) { exit 1 }
    }
    else {
        Write-Error "junction error: $link exists and is not a junction"
        exit 2
    }
}

Write-Host "junction create: $link -> $repo"
cmd /c mklink /J "$link" "$repo"
exit $LASTEXITCODE
