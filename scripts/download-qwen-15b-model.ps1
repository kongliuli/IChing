param(
    [string]$Target = "./models/qwen2.5-1.5b-genai",
    [string]$Repo = "tonythethompson/Qwen2.5-1.5B-Instruct-ONNX"
)

$ErrorActionPreference = "Stop"
$files = @(
    ".gitattributes",
    "README.md",
    "added_tokens.json",
    "chat_template.jinja",
    "config.json",
    "genai_config.json",
    "generation_config.json",
    "merges.txt",
    "model.onnx",
    "model.onnx.data",
    "quantize_config.json",
    "special_tokens_map.json",
    "tokenizer.json",
    "tokenizer_config.json",
    "vocab.json"
)

New-Item -ItemType Directory -Force -Path $Target | Out-Null

foreach ($file in $files) {
    $out = Join-Path $Target $file
    if (Test-Path $out) {
        Write-Host "exists $file"
        continue
    }

    $uriFile = [Uri]::EscapeDataString($file).Replace("%2F", "/")
    $uri = "https://huggingface.co/$Repo/resolve/main/$uriFile"
    Write-Host "download $file"
    Invoke-WebRequest -Uri $uri -OutFile $out
}

if (-not (Test-Path (Join-Path $Target "genai_config.json"))) {
    throw "genai_config.json not found; this repo may not be ORT GenAI compatible."
}

Write-Host "Model downloaded to $Target"
