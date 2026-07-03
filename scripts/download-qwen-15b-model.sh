#!/usr/bin/env bash
# Qwen2.5-1.5B-Instruct — ONNX Runtime GenAI format
set -euo pipefail
TARGET="${1:-./models/qwen2.5-1.5b-genai}"
REPO="${2:-tonythethompson/Qwen2.5-1.5B-Instruct-ONNX}"
mkdir -p "$(dirname "$TARGET")"

download() {
  if command -v huggingface-cli >/dev/null 2>&1; then
    huggingface-cli download "$REPO" --local-dir "$TARGET"
  elif command -v hf >/dev/null 2>&1; then
    hf download "$REPO" --local-dir "$TARGET"
  else
    pip install -q huggingface_hub
    python - <<PY
from huggingface_hub import snapshot_download
snapshot_download(repo_id="${REPO}", local_dir="${TARGET}")
PY
  fi
}

download

if [[ ! -f "$TARGET/genai_config.json" ]]; then
  echo "ERROR: genai_config.json not found — this repo may not be ORT GenAI compatible." >&2
  echo "See docs/inference-layer-design.md §6.2 for alternatives." >&2
  exit 1
fi

echo "Model downloaded to ${TARGET}"
echo "Verify: ls ${TARGET}/genai_config.json model.onnx tokenizer.json"
