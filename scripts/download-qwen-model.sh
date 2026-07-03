#!/usr/bin/env bash
set -euo pipefail
TARGET="${1:-./models/qwen3-0.6b-genai}"
mkdir -p "$(dirname "$TARGET")"

if command -v huggingface-cli >/dev/null 2>&1; then
  huggingface-cli download xiaoyao9184/Qwen3-0.6B-onnx-genai --local-dir "$TARGET"
elif command -v hf >/dev/null 2>&1; then
  hf download xiaoyao9184/Qwen3-0.6B-onnx-genai --local-dir "$TARGET"
else
  pip install -q huggingface_hub
  python - <<PY
from huggingface_hub import snapshot_download
snapshot_download(repo_id="xiaoyao9184/Qwen3-0.6B-onnx-genai", local_dir="${TARGET}")
PY
fi

echo "Model downloaded to ${TARGET}"
