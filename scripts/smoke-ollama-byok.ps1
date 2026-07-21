# Smoke: Byok OpenAI-compatible path against local Ollama (no API key).
# Usage: .\scripts\smoke-ollama-byok.ps1
param(
    [string]$BaseUrl = "http://localhost:11434/v1",
    [string]$Model = "qwen3.5:9b"
)

$ErrorActionPreference = "Stop"
$uri = "$($BaseUrl.TrimEnd('/'))/chat/completions"
# think:false — Qwen3.5 otherwise fills reasoning and leaves content empty under low max_tokens
$payload = @{
    model = $Model
    messages = @(
        @{ role = "user"; content = "Reply with exactly: pong" }
    )
    temperature = 0.1
    max_tokens = 256
    stream = $false
    think = $false
}
$body = $payload | ConvertTo-Json -Depth 6

Write-Host "POST $uri model=$Model (no Authorization, think=false)"
try {
    $resp = Invoke-RestMethod -Uri $uri -Method Post -ContentType "application/json; charset=utf-8" -Body $body -TimeoutSec 180
} catch {
    Write-Error "Ollama smoke failed: $_. Ensure 'ollama serve' is running and model is pulled: ollama pull $Model"
    exit 1
}

$content = $resp.choices[0].message.content
Write-Host "OK content=$content"
if ([string]::IsNullOrWhiteSpace($content)) {
    Write-Error "Empty content from model"
    exit 1
}
exit 0
