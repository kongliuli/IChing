# 将 Qwen3.5 HF 权重导出为 ORT GenAI 目录（含 genai_config.json）
# 失败时请看 docs/active/qwen35-genai.md
param(
    [string]$ModelId = "Qwen/Qwen3.5-0.8B",
    [string]$OutDir = "./models/qwen3.5-0.8b-genai",
    [ValidateSet("int4", "fp16", "fp32")]
    [string]$Precision = "int4",
    [ValidateSet("cpu", "cuda", "dml")]
    [string]$Ep = "cpu"
)

$ErrorActionPreference = "Stop"
Write-Host "Model: $ModelId"
Write-Host "Out:   $OutDir"
Write-Host "Prec:  $Precision / EP: $Ep"
Write-Host ""
Write-Host "注意：onnx-community 的 *-ONNX 包是 Transformers.js 格式，不能替代本脚本产物。"
Write-Host ""

python -c "import onnxruntime_genai" 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "正在安装 onnxruntime-genai ..."
    pip install onnxruntime-genai
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

python -m onnxruntime_genai.models.builder `
    -m $ModelId `
    -o $OutDir `
    -p $Precision `
    -e $Ep

$config = Join-Path $OutDir "genai_config.json"
if (-not (Test-Path $config)) {
    throw "导出后未找到 genai_config.json。可能是当前 GenAI 版本尚不支持该 Qwen3.5 架构，见 docs/active/qwen35-genai.md"
}

Write-Host "OK: $config"
Write-Host "下一步: PromptTest --model $OutDir ，再把 OnnxModelPackCatalog 加上该目录的文件清单。"
